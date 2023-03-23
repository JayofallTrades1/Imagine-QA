using System;
using System.Collections.Generic;
using System.Linq;
using Onyx.DataAccess.ScheduleAccess.Operations;
using Onyx.DataAccess.ScheduleAccess;
using Onyx.OSD.Scheduling;
using Onyx.Services.IntegrationService;
using Onyx.Services.IntegrationService.Extensions;
using Versio.Automation.Services.Rules.Helpers;
using VersioTimecode;

namespace Versio.Automation.Services.Rules
{
    // Rules: AMC Latam 09282022.001
    public class Rules : IRules
    {
        /*
        GOUN 1
        GOUS 2
        GOUU 3
        FYAL 4
        FYAM 5
        FYAB 6
        UT07 7
        UT08 8
        MASL 9
        MASU 10
        AMCN 11
        AMCS 12
        AMCL 13
        AMCB 14
        AMCO 15
        EURL 16
        UT17 17
        UT18 18
        */

        private static string localStartMarker = "LOCAL_START";
        private static string localEndMarker = "LOCAL_END";
        private uint _spliceEventId;
        private string _channelName;
        private static List<string> _dtmfChar473 = new List<string> { "GOUN", "GOUS", "GOUU", "EURL", "FYAL", "FYAM", "FYAB", "UT07", "OT08" };
        private static string _dtmfChar473Start = "473*";
        private static string _dtmfChar473End = "473#";
        private static List<string> _dtmfChar429 = new List<string> { "AMCN", "AMCS", "AMCL", "AMCB", "AMCO", "MASL", "MASU", "UT17", "UT18" };
        private static string _dtmfChar429Start = "429*";
        private static string _dtmfChar429End = "429#";
        private string _dtmfCharStart;
        private string _dtmfCharEnd;
        private static string logStart = "LOG-START:";
        private static string customPropertyGaphicLayer = "NonPrimaryEvent_ImagineCommunications_1_GraphicLayer_1";


        public IEnumerable<Schedule> ApplyRules(IEnumerable<Schedule> input)
        {
            if (input == null) return input;
            var output = new List<Schedule>();
            foreach (var schedule in input)
            {
                if (schedule == null) continue;
                schedule.UIdAsGuid = Guid.NewGuid();
                var activeContainers = new Stack<ScheduleContainer>();
                _channelName = schedule.PlayoutChannel;
                if (_dtmfChar473.Contains(_channelName))
                {
                    _dtmfCharStart = _dtmfChar473Start;
                    _dtmfCharEnd = _dtmfChar473End;
                }
                else if (_dtmfChar429.Contains(_channelName))
                {
                    _dtmfCharStart = _dtmfChar429Start;
                    _dtmfCharEnd = _dtmfChar429End;
                }

                for (int i = 0; i < schedule.ScheduledEvents.Count; i++)
                {
                    var evt = schedule.ScheduledEvents[i];

                    if (evt.IsPrimary && evt.PrimaryContent.FunctionalType == FunctionalType.Program)
                    {
                        evt.ManuallyUpdated = true;
                    }

                    GraphicSupport(schedule, ref evt);

                    ContainerSupport(schedule, activeContainers, i);

                    ContainerSCTEEventSupport(schedule, evt);

                    i = UnsupportedEvents(schedule, i);
                }
                schedule.ScheduleType = ScheduleType.Offline;
                output.Add(schedule);
				// Duplicate offline schedule
				/* 
                if(!_channelName.Contains("-U"))
                {
                    // Offline schedule.
                    var offlineSchedule = CloneSchedule(schedule);
                    output.Add(offlineSchedule);
                }*/
            }

            return output;
        }

        private static ScheduleWithOperation CloneSchedule(Schedule schedule)
        {
            var baseSchedule = schedule as ScheduleWithOperation;
            var offlineSchedule = baseSchedule.ShallowCopyWithoutEvents();
            offlineSchedule.UIdAsGuid = Guid.NewGuid();
            offlineSchedule.ScheduleType = ScheduleType.Offline;
            offlineSchedule.Containers = schedule.Containers;
            offlineSchedule.AppendEvents(schedule.ScheduledEvents);
            offlineSchedule.Operation = baseSchedule.Operation;
            offlineSchedule.InsertAfterTarget = baseSchedule.InsertAfterTarget;

            return offlineSchedule;
        }

        private static int UnsupportedEvents(Schedule schedule, int i)
        {
            var evt = schedule.ScheduledEvents[i];
            if ((evt.Type == ScheduledEventType.CommentEvent && !evt.Title.StartsWith(logStart)) ||
                evt.Type == ScheduledEventType.UnsupportedEvent)
            {
                schedule.ScheduledEvents.RemoveAt(i);
                i--;
            }

            return i;
        }

        private void GraphicSupport(Schedule schedule, ref ScheduledEvent evt)
        {
            var customProperties = evt.CustomProperties;
            if (evt.BasicEventDescriptor == BasicEventDescriptor.SecondaryEvent && 
                evt.Type == ScheduledEventType.GraphicsEvent &&
                customProperties.ContainsKey(customPropertyGaphicLayer))
            {
                var layerStr = customProperties.FirstOrDefault(kvp => kvp.Key.Contains(customPropertyGaphicLayer)).Value.ToString();
                int layerNumber;
                bool isNumber = int.TryParse(layerStr, out layerNumber);
                if(isNumber && layerNumber >= 1 && layerNumber <= 10)
                {
                    evt.SecondaryEventData.GraphicsLayer = layerStr;
                }
            }
        }

        private void ContainerSupport(Schedule schedule, Stack<ScheduleContainer> activeContainers, int i)
        {
            var evt = schedule.ScheduledEvents[i];

            SupportLocalBreakContainer(schedule, i, activeContainers);

            if (activeContainers.Count > 0)
            {
                ScheduleContainer activeContainer = activeContainers.Peek();
                if (activeContainer != null && (evt.Type == ScheduledEventType.NonProgramEvent || evt.Type == ScheduledEventType.ProgramEvent))
                {
                    if (evt.ParentId != null)
                    {
                        var currentContainer = schedule.GetContainer(evt.ParentId.Value);
                        currentContainer.RemoveChild(evt.UIdAsGuid);
                    }
                    evt.ParentId = activeContainer.UId;

                    activeContainer.AppendChild(evt.UIdAsGuid);
                }
            }
        }

        private void SupportLocalBreakContainer(Schedule schedule, int i, Stack<ScheduleContainer> activeContainers)
        {
            var evt = schedule.ScheduledEvents[i];

            if (schedule.ScheduledEvents[i].BasicEventDescriptor != BasicEventDescriptor.PrimaryEvent) return;

            if (activeContainers != null && activeContainers.Count > 0 && schedule.ScheduledEvents[i].ParentId != activeContainers.Peek().ParentContainer)
            {
                activeContainers.Pop();
            }

            for (var j = 1; i + j < schedule.ScheduledEvents.Count && schedule.ScheduledEvents[i + j].BasicEventDescriptor != BasicEventDescriptor.PrimaryEvent; j++)
            {
                var nextEvent = schedule.ScheduledEvents[i + j] as UnsupportedEvent;
                if (nextEvent == null) continue;
                if (nextEvent.SecondaryEventData == null || nextEvent.SecondaryEventData.PrimaryEventId != schedule.ScheduledEvents[i].UId) return;

                var typeName = nextEvent.OriginalTypeName;
                if (typeName == localEndMarker)
                {
                    if (activeContainers.Count > 0)
                    {
                        activeContainers.Pop();
                    }
                }
                else if (typeName == localStartMarker)
                {
                    ScheduleContainer networkContainer = null;
                    var localContainerName = "";
                    var primary = schedule.ScheduledEvents[i];
                    if (primary != null && primary.ParentId != null)
                    {
                        networkContainer = schedule.GetContainer(primary.ParentId.Value);
                        var networkContainerNameParts = networkContainer.Name.Split(' ');
                        localContainerName = networkContainerNameParts.Length >= 2 ?
                            string.Format("{0} {1}", networkContainerNameParts[0], networkContainerNameParts[1]) : "";
                    }
                    var localContainer = new ScheduleContainer()
                    {
                        ContainerType = FunctionalType.Commercial,
                        Name = localContainerName != "" ? localContainerName + " Local" : "Local",
                        CType = ContainerType.Affiliate,
                        //45GACType = ContainerType.Normal,
                        UId = nextEvent.UIdAsGuid
                    };
                    AddContainerToSchedule(schedule, activeContainers, networkContainer, localContainer, primary != null ? primary.UIdAsGuid : Guid.Empty);
                }
            }
        }

        private static void AddContainerToSchedule(Schedule schedule, Stack<ScheduleContainer> activeContainers, ScheduleContainer networkContainer, ScheduleContainer localContainer, Guid primaryId)
        {
            AddContainerLinks(networkContainer, localContainer, primaryId);

            schedule.Containers.Add(localContainer.UId.Value, localContainer);

            activeContainers.Push(localContainer);
        }

        private static void AddContainerLinks(ScheduleContainer networkContainer, ScheduleContainer localContainer, Guid primaryId)
        {
            if (networkContainer != null)
            {
                localContainer.ParentContainer = networkContainer.UId;

                if (networkContainer == null || primaryId == Guid.Empty)
                {
                    networkContainer.AppendChild(localContainer.UId.Value);
                }
                else
                {
                    var childIndex = networkContainer.Children.IndexOf(primaryId);

                    if (networkContainer == null || primaryId == Guid.Empty || childIndex >= networkContainer.Children.Count)
                    {
                        networkContainer.AppendChild(localContainer.UId.Value);
                    }
                    else if (childIndex < 1)
                    {
                        networkContainer.Children.Insert(0, localContainer.UId.Value);
                    }
                    else
                    {
                        networkContainer.Children.Insert(childIndex, localContainer.UId.Value);
                    }
                }
            }
        }

        private void ContainerSCTEEventSupport(Schedule schedule, ScheduledEvent evt)
        {
            if (evt.ParentId == null || evt.BasicEventDescriptor != BasicEventDescriptor.PrimaryEvent) return;

            var parent = schedule.GetContainer(evt.ParentId.Value);

            if (parent == null || parent.Children == null) return;

            var firstPrimary = GetFirstPrimary(schedule, parent);
            if (firstPrimary != evt.UIdAsGuid) return;

            var containers = new List<ScheduleContainer>();
            containers.Add(parent);

            if (parent.ContainerType != FunctionalType.Program && parent.ParentContainer != null)
            {
                var lastId = parent.UId;
                for (var container = schedule.GetContainer(parent.ParentContainer.Value);
                    GetFirstPrimary(schedule, container, lastId) == lastId;
                    container = schedule.GetContainer(container.ParentContainer.Value))
                {
                    containers.Add(container);
                    if (container.ContainerType == FunctionalType.Program || container.ParentContainer == null)
                    {
                        break;
                    }
                    lastId = container.UId;
                }
            }
            else if (parent.ContainerType == FunctionalType.Program)
            {
                if (HasProgramDescendants(schedule, parent))
                {
                    // No SCTE messages need to be made for this container.
                    return;
                }
            }

            foreach (var container in containers)
            {
                if (container.ContainerType == FunctionalType.Program)
                {
                    // No SCTE messages need to be made for this container.
                }
                else if (container.ContainerType == FunctionalType.Commercial
                    && (container.CType == ContainerType.BreakPod || container.CType == ContainerType.BonusPod))
                {
                    // No SCTE messages need to be made for this container.
                }
                else if (container.ContainerType == FunctionalType.Commercial && container.CType == ContainerType.Affiliate)
                //45GA else if (container.ContainerType == FunctionalType.Commercial && container.CType == ContainerType.Normal)
                {
                    // Local Break Start
                    var messagesStart = new List<SCTEMessageBase>();
                    ushort startPreRollMs = 8000;
                    var startPreRoll10Sec = (byte)(startPreRollMs / 100);

                    messagesStart.Add(new SCTESpliceMessage()
                    {
                        ScheduleType = SCTEEvent.SCTEScheduleType.SpliceStartOnly,
                        TriggerType = SCTEEvent.SCTEType.SCTENormal,
                        SpliceEventId = _spliceEventId++,
                        PreRollTime = startPreRollMs

                    });
                    messagesStart.Add(new SCTEDTMFMessage()
                    {
                        ScheduleType = SCTEEvent.SCTEScheduleType.SpliceStartOnly,
                        Preroll = startPreRoll10Sec,
                        DTMF = _dtmfCharStart,
                        EndDTMF = _dtmfCharEnd
                    });
                    var newMessageStart = CreateAndInsertSCTEMulti(schedule, evt.UIdAsGuid, container, messagesStart, "Local Break Start");
                    container.SecondaryIds.Add(newMessageStart.UIdAsGuid);

                    // Local Break End
                    var messagesEnd = new List<SCTEMessageBase>();
                    ushort endPreRollMs = 0;
                    var endPreRoll10Sec = (byte)(endPreRollMs / 100);
                    messagesEnd.Add(new SCTESpliceMessage()
                    {
                        ScheduleType = SCTEEvent.SCTEScheduleType.SpliceStopOnly,
                        TriggerType = SCTEEvent.SCTEType.SCTENormal,
                        SpliceEventId = _spliceEventId++,
                        PreRollTime = endPreRollMs

                    });
                    messagesEnd.Add(new SCTEDTMFMessage()
                    {
                        ScheduleType = SCTEEvent.SCTEScheduleType.SpliceStopOnly,
                        Preroll = endPreRoll10Sec,
                        DTMF = _dtmfCharStart,
                        EndDTMF = _dtmfCharEnd,
                    });
                    var newMessageEnd = CreateAndInsertSCTEMulti(schedule, evt.UIdAsGuid, container, messagesEnd, "Local Break End");
                    container.SecondaryIds.Add(newMessageEnd.UIdAsGuid);
                }
            }
        }
        private Guid? GetFirstPrimary(Schedule schedule, ScheduleContainer parent, Guid? matchingGuid = null)
        {
            // Track all the way back because the containers reuse guids (what a mess... that was a terrible idea Andrew)
            var containerPath = new HashSet<Guid>();
            var eventMap = schedule.ScheduledEvents.ToDictionary(evt => evt.UIdAsGuid);
            var toCheck = new Stack<Guid>();
            toCheck.Push(parent.UId.Value);
            while (toCheck.Count > 0)
            {
                var id = toCheck.Pop();
                // Break out early when requested
                if (matchingGuid == id) return id;

                var container = schedule.GetContainer(id);
                if (container != null)
                {
                    containerPath.Add(container.UId.Value);
                    var children = container.Children.Read();
                    //45GA var children = container.Children;
                    children.Reverse();
                    foreach (var child in children)
                    {
                        // Reusing Ids of events for containers makes things tough.
                        if (containerPath.Contains(child)) continue;
                        toCheck.Push(child);
                    }
                    continue;
                }
                ScheduledEvent evt;
                if (eventMap.TryGetValue(id, out evt) && evt.BasicEventDescriptor == BasicEventDescriptor.PrimaryEvent)
                {
                    return id;
                }
            }
            return null;
        }

        private static bool HasProgramDescendants(Schedule schedule, ScheduleContainer parent)
        {
            // Make sure there isn't a lower level program container to negate the need for SCTE messages
            // Note that with the current implementation there should never be any
            bool hasProgramDescendants = false;
            var children = parent.Children.Read();
            //45GA var children = parent.Children;
            while (children.Count > 0)
            {
                // Breadth vs depth search doesn't really matter since it probably has to traverse the whole tree anyways
                var container = schedule.GetContainer(children[children.Count - 1]);
                children.RemoveAt(children.Count - 1);
                if (container == null) continue;

                if (container.ContainerType == FunctionalType.Program)
                {
                    hasProgramDescendants = true;
                    break;
                }
                children.AddRange(container.Children.Read());
                //45GA children.AddRange(container.Children);
            }

            return hasProgramDescendants;
        }

        private SCTEMultiEvent CreateAndInsertSCTEMulti(Schedule schedule, Guid parentId, ScheduleContainer container, List<SCTEMessageBase> baseMessages, string messagePurpose)
        {
            SCTEMultiEvent newMessage = new SCTEMultiEvent();
            newMessage.PrimaryContent = new ContentInfo();
            newMessage.PrimaryContent.FunctionalType = FunctionalType.Commercial;
            newMessage.PrimaryContent.SegmentNumber = 0;
            newMessage.PrimaryContent.Title = "SCTE " + messagePurpose;
            newMessage.PrimaryContent.HouseId = "SCTE_" + messagePurpose;
            newMessage.PrimaryContent.ContentType = ContentType.Other;
            newMessage.BasicEventDescriptor = BasicEventDescriptor.SecondaryEvent;
            newMessage.SecondaryEventData = new SecondaryEventData();
            newMessage.SecondaryEventData.PrimaryEventId = parentId.ToString();
            newMessage.SecondaryEventData.SecondaryType = SecondaryEventType.MatchContainer;
            newMessage.Title = "SCTE " + messagePurpose;
            newMessage.TimestampType = TimestampType.None;
            newMessage.ParentId = container.UId;
            var newMessages = new List<SCTEMessageBase>(baseMessages);
            newMessage.Messages = newMessages;
            schedule.InsertEvents(parentId, new ScheduledEvent[] { newMessage }, false, false, false);
            return newMessage;
        }
    }
}