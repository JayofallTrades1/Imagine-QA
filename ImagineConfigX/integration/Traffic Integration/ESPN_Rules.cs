using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Onyx.OSD.Scheduling;
using Onyx.Services.IntegrationService;
using Onyx.Services.IntegrationService.Extensions;
using Onyx.DataAccess.ScheduleAccess.Operations;
using VersioTimecode;

namespace Versio.Automation.Services.Rules
{
    class Rules : IRules
    {
        private const string c_bonusMarker = "Bonus Break";
        private const string c_PlaceholderHouseId = "Unknown";

        private uint s_SegmentId;
        private Random m_random = new Random();

        public IEnumerable<Schedule> ApplyRules(IEnumerable<Schedule> input)
        {
            if (input == null) return input;

            foreach (var schedule in input)
            {
                if (schedule == null) continue;

                string currentNetworkIdent = null;
                string currentRegional = null;

                // This only accounts for container modifications to the base containers already created.
                var activeContainers = new Stack<ScheduleContainer>();

                for (int i = 0; i < schedule.ScheduledEvents.Count; i++)
                {
                    // Continuously update the list to account for added events
                    var evt = schedule.ScheduledEvents[i];

                    ContainerSupport(schedule, activeContainers, i);

                    ContainerSCTEEventSupport(schedule, evt);

                    SetEventPassthrough(evt);

                    TransitionSupport(evt);

                    AudioProfileSupport(evt);

                    NetworkIdentSupport(evt, ref currentNetworkIdent);

                    RegionalSupport(schedule, i, evt, ref currentRegional);

                    PlaceholderHouseIds(schedule, i, evt);

                    ClearUnnecessary(schedule, ref i);
                }

                FinalizeRegionalSupport(schedule, currentRegional);

                ConsolidateAiringId(schedule);

                schedule.ScheduleType = ScheduleType.Offline;
                schedule.UId = Guid.NewGuid().ToString();
            }

            return input;
        }

        private void PlaceholderHouseIds(Schedule schedule, int i, ScheduledEvent evt)
        {
            if (evt != null && evt.BasicEventDescriptor == BasicEventDescriptor.PrimaryEvent && String.IsNullOrEmpty(evt.PrimaryContent.HouseId))
            {
                evt.PrimaryContent.HouseId = c_PlaceholderHouseId;
                evt.IsPlaceholder = true;
            }
        }

        private void FinalizeRegionalSupport(Schedule schedule, string currentRegional)
        {
            if (schedule == null || schedule.ScheduledEvents == null || currentRegional == null) return;
            var events = schedule.ScheduledEvents;
            for (int i = events.Count - 1; i > 0; i--)
            {
                if (events[i] == null || events[i].Type != ScheduledEventType.ProgramEvent) continue;
                if (!String.IsNullOrEmpty(events[i].GangName))
                {
                    // Make the last program event leave the gang
                    events[i].GangId = String.Empty;
                }
                break;
            }
        }

        private void RegionalSupport(Schedule schedule, int i, ScheduledEvent evt, ref string currentRegional)
        {
            var reference = GetESPNProperty(evt, "RegionReference");
            // Compare if the gang changes before actually changing the gang.
            if (evt.Type == ScheduledEventType.ProgramEvent && (String.IsNullOrEmpty(reference) || (currentRegional != null && currentRegional != reference)))
            {
                // Go back and find the most recent segment of the previous program to leave the gang.
                for (var j = i - 1; j >= 0; j--)
                {
                    if (!String.IsNullOrEmpty(schedule.ScheduledEvents[j].GangName))
                    {
                        schedule.ScheduledEvents[j].GangId = String.Empty;
                        currentRegional = null;
                        break;
                    }
                    if (evt.Type == ScheduledEventType.ProgramEvent)
                    {
                        break;
                    }
                }
            }
            if (!String.IsNullOrEmpty(reference))
            {
                evt.GangName = reference;
                currentRegional = reference;
            }
            var master = GetESPNProperty(evt, "RegionMaster");
            bool masterResult;
            if (bool.TryParse(master, out masterResult) && masterResult)
            {
                evt.IsGangMaster = true;
            }
            else
            {
                evt.IsGangMaster = false;
            }
        }

        private void ContainerSCTEEventSupport(Schedule schedule, ScheduledEvent evt)
        {
            if (evt.ParentId == null || evt.BasicEventDescriptor != BasicEventDescriptor.PrimaryEvent) return;

            int msOffset = GetRequestedPreroll(evt, ConvertTimecodeKind(schedule.Framerate));
            if (msOffset > 0)
            {
                evt.CustomProperties.Add(CustomProperty.Keys.ESPNSCTEPreroll, msOffset.ToString());
            }

            var parent = schedule.GetContainer(evt.ParentId.Value);

            if (parent == null || parent.Children == null) return;

            var firstPrimary = GetFirstPrimary(schedule, parent);
            if (firstPrimary != evt.UIdAsGuid) return;

            var containers = new List<ScheduleContainer>();
            containers.Add(parent);

            if (parent.ContainerType != FunctionalType.Program && parent.ParentContainer != null)
            {
                // Get all the containers up to the first Program container which all share the same first primary event
                var lastId = parent.UId;
                for (var container = schedule.GetContainer(parent.ParentContainer.Value);
                    GetFirstPrimary(schedule, container, lastId) == lastId;
                    container = schedule.GetContainer(container.ParentContainer.Value))
                {
                    containers.Add(container);
                    // Do this at the end only
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
                    // No SCTE messages need to be made for this container
                    return;
                }
            }

            // Get the airing id
            string airingId = GetAiringId(schedule, containers);

            foreach (var container in containers)
            {
                if (container.ContainerType == FunctionalType.Program)
                {
                    object msOffsetObj = null;
                    ushort prerollMS = 2000;
                    if (!(container.CustomProperties.TryGetValue(CustomProperty.Keys.ESPNSCTEPreroll, out msOffsetObj) && ushort.TryParse(msOffsetObj.ToString(), out prerollMS)))
                    {
                        prerollMS = 2000;
                    }
                    var messages = new List<SCTEMessageBase>();
                    messages.Add(new SCTETimeSignalMessage()
                    {
                        PreRollTime = prerollMS
                    });
                    messages.Add(new SCTESegmentDescriptorMessage()
                    {
                        SegmentationId = s_SegmentId++, // Should update after assignment but really doesn't matter as long as it's consistent
                        IncludeDuration = false,
                        SegmentationUpidType = SCTESegmentDescriptorMessage.UpidType.UserDefined,
                        SegmentationUpid = airingId == null ? new byte[0] : System.Text.Encoding.UTF8.GetBytes(airingId),
                        SegmentationTypeId = SCTESegmentDescriptorMessage.SegmentationType.ProgramStart,
                        SegmentNumber = 0,
                        SegmentsExpected = 0,
                        DeliveryRestrictions = null,
                    });

                    var multi = CreateAndInsertSCTEMulti(schedule, evt.UIdAsGuid, container, messages, "Program", "PGM");
                    multi.SecondaryEventData.OffsetFromPrimaryStart = 1;
                    multi.SecondaryEventData.OffsetFromPrimaryEnd = -1;
                    container.SecondaryIds.Add(multi.UIdAsGuid);
                    // They have the same info and the first attempt to serialize them will create separate objects
                    // Be careful setting properties on the messages before then.
                    multi = CreateAndInsertSCTEMulti(schedule, evt.UIdAsGuid, container, messages, "Program", "C-Feed");
                    multi.SecondaryEventData.OffsetFromPrimaryStart = 1;
                    multi.SecondaryEventData.OffsetFromPrimaryEnd = -1;
                    container.SecondaryIds.Add(multi.UIdAsGuid);
                    // The 2 program containers do not need to update the airing id because it cannot be updated
                }
                else if (container.ContainerType == FunctionalType.Commercial && container.CType == ContainerType.BreakPod)
                {
                    object msOffsetObj = null;
                    ushort prerollMS = 2000;
                    if (!(container.CustomProperties.TryGetValue(CustomProperty.Keys.ESPNSCTEPreroll, out msOffsetObj) && ushort.TryParse(msOffsetObj.ToString(), out prerollMS)))
                    {
                        prerollMS = 2000;
                    }
                    var messages = new List<SCTEMessageBase>();
                    messages.Add(new SCTETimeSignalMessage()
                    {
                        PreRollTime = prerollMS
                    });
                    messages.Add(new SCTEDTMFMessage()
                    {
                        Preroll = (byte)(prerollMS / 100),
                        DTMF = "#*02",
                        EndDTMF = "#*03",
                    });
                    messages.Add(new SCTESegmentDescriptorMessage()
                    {
                        SegmentationId = s_SegmentId++, // Should update after assignment but really doesn't matter as long as it's consistent
                        IncludeDuration = true,
                        SegmentationUpidType = SCTESegmentDescriptorMessage.UpidType.UserDefined,
                        SegmentationUpid = airingId == null ? new byte[0] : System.Text.Encoding.UTF8.GetBytes(airingId),
                        SegmentationTypeId = SCTESegmentDescriptorMessage.SegmentationType.BreakStart,
                        SegmentNumber = 0,  // The sample has these left 0 - I like having less work to do.
                        SegmentsExpected = 0,  // The sample has these left 0 - I like having less work to do.
                        DeliveryRestrictions = null,
                    });

                    var newMessage = CreateAndInsertSCTEMulti(schedule, evt.UIdAsGuid, container, messages, "Break", "PGM");
                    // The value doesn't actually matter, only the existance
                    container.CustomProperties.Add(CustomProperty.Keys.ESPNToUpdateAiringID, "true");
                    container.SecondaryIds.Add(newMessage.UIdAsGuid);
                }
                else if (container.ContainerType == FunctionalType.Commercial && container.CType == ContainerType.Normal)
                {
                    object msOffsetObj = null;
                    ushort prerollMS = 6000;
                    if (!(container.CustomProperties.TryGetValue(CustomProperty.Keys.ESPNSCTEPreroll, out msOffsetObj) && ushort.TryParse(msOffsetObj.ToString(), out prerollMS)))
                    {
                        prerollMS = 6000;
                    }
                    var segmentId = s_SegmentId++;
                    var messages = new List<SCTEMessageBase>();
                    messages.Add(new SCTESpliceMessage()
                    {
                        SpliceEventId = segmentId,
                        // Should probably make this match all the other program ids of SCTE messages in this program.
                        // But that isn't explicitly mentioned in the JIRA case VPS-5971.
                        UniqueProgramId = (ushort)m_random.Next(UInt16.MinValue, UInt16.MaxValue),
                        BreakDuration = 0,  // Let the driver auto-calculate
                        AvailNum = 1,  // Might want to do more here to correctly calculate
                        AvailsExpected = 1,  // Might want to do more here to correctly calculate
                        PreRollTime = prerollMS,
                    });
                    messages.Add(new SCTEDTMFMessage()
                    {
                        Preroll = (byte)(prerollMS / 100),
                        DTMF = "#*00",
                        EndDTMF = "#*01",
                    });
                    messages.Add(new SCTESegmentDescriptorMessage()
                    {
                        SegmentationId = segmentId, // Should update after assignment but really doesn't matter as long as it's consistent
                        IncludeDuration = true,
                        SegmentationUpidType = SCTESegmentDescriptorMessage.UpidType.UserDefined,
                        SegmentationUpid = airingId == null ? new byte[0] : System.Text.Encoding.UTF8.GetBytes(airingId),
                        SegmentationTypeId = SCTESegmentDescriptorMessage.SegmentationType.DistributorPlacementOpportunityStart,
                        SegmentNumber = 0,  // The sample has these left 0 - I like having less work to do.
                        SegmentsExpected = 0,  // The sample has these left 0 - I like having less work to do.
                        DeliveryRestrictions = null,
                    });

                    var newMessage = CreateAndInsertSCTEMulti(schedule, evt.UIdAsGuid, container, messages, "Affiliate", "PGM");
                    // The value doesn't actually matter, only the existance
                    container.CustomProperties.Add(CustomProperty.Keys.ESPNToUpdateAiringID, "true");
                    container.SecondaryIds.Add(newMessage.UIdAsGuid);
                }
            }
        }

        private int GetRequestedPreroll(ScheduledEvent evt)
        {
            var timecodeOffset = GetESPNProperty(evt, "StartTimeOffset");
            return GetRequestedPreroll(evt.Duration.Kind, ref timecodeOffset);
        }

        private int GetRequestedPreroll(ScheduledEvent evt, TimecodeKind timecodeKind)
        {
            var timecodeOffset = GetESPNProperty(evt, "StartTimeOffset");
            return GetRequestedPreroll(timecodeKind, ref timecodeOffset);
        }

        private int GetRequestedPreroll(ScheduleContainer container, TimecodeKind timecodeKind)
        {
            var timecodeOffset = GetESPNProperty(container, "StartTimeOffset");
            return GetRequestedPreroll(timecodeKind, ref timecodeOffset);
        }

        private static int GetRequestedPreroll(TimecodeKind timecodeKind, ref string timecodeOffset)
        {
            if (String.IsNullOrWhiteSpace(timecodeOffset)) { return -1; }

            timecodeOffset = timecodeOffset.Trim();
            bool negative = timecodeOffset[0] == '-';
            if (!negative) { return -1; }
            timecodeOffset = timecodeOffset.Substring(1);

            var hours = int.Parse(timecodeOffset.Substring(0, 2));
            var minutes = int.Parse(timecodeOffset.Substring(3, 2));
            var seconds = int.Parse(timecodeOffset.Substring(6, 2));
            var frames = int.Parse(timecodeOffset.Substring(9, 2));
            var timecode = new Timecode(hours, minutes, seconds, frames, timecodeKind);
            var ms = timecode.TotalMilliseconds;
            if (ms >= ushort.MinValue && ms <= ushort.MaxValue)
            {
                return (int)ms;
            }

            return -1;
        }

        private static bool HasProgramDescendants(Schedule schedule, ScheduleContainer parent)
        {
            // Make sure there isn't a lower level program container to negate the need for SCTE messages
            // Note that with the current implementation there should never be any
            bool hasProgramDescendants = false;
            var children = new List<Guid>(parent.Children);
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
                children.AddRange(container.Children);
            }

            return hasProgramDescendants;
        }

        private static string GetAiringId(Schedule schedule, List<ScheduleContainer> containers)
        {
            string airingId = null;
            for (var container = containers[containers.Count - 1]; ;)
            {
                object property;
                if (container.CustomProperties.TryGetValue(CustomProperty.Keys.ESPNAiringID, out property))
                {
                    airingId = property as string;
                    break;
                }

                if (container.ParentContainer == null) break;
                container = schedule.GetContainer(container.ParentContainer.Value);
            }

            return airingId;
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
                    var children = new List<Guid>(container.Children);
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

        private SCTEMultiEvent CreateAndInsertSCTEMulti(Schedule schedule, Guid parentId, ScheduleContainer container, List<SCTEMessageBase> baseMessages, string messagePurpose, string feed)
        {
            var effectiveFeed = String.IsNullOrEmpty(feed) ? "Main" : feed;
            SCTEMultiEvent newMessage = new SCTEMultiEvent();
            newMessage.PrimaryContent = new ContentInfo();
            //newSwitch.PrimaryContent.HouseId = target;
            newMessage.PrimaryContent.FunctionalType = FunctionalType.Commercial;
            newMessage.PrimaryContent.SegmentNumber = 0;
            newMessage.PrimaryContent.Title = "SCTE " + messagePurpose + " for " + effectiveFeed;
            newMessage.PrimaryContent.HouseId = "SCTE_" + messagePurpose + "_for_" + effectiveFeed;
            newMessage.PrimaryContent.ContentType = ContentType.Other;
            newMessage.BasicEventDescriptor = BasicEventDescriptor.SecondaryEvent;
            newMessage.SecondaryEventData = new SecondaryEventData();
            newMessage.SecondaryEventData.PrimaryEventId = parentId.ToString();
            newMessage.SecondaryEventData.SecondaryType = SecondaryEventType.MatchContainer;
            newMessage.Title = "SCTE " + messagePurpose + " for " + effectiveFeed;
            newMessage.TimestampType = TimestampType.None;
            newMessage.ParentId = container.UId;
            if (!String.IsNullOrEmpty(feed))
            {
                newMessage.Feed.Add(feed);
            }
            //SCTE Specific
            // For now, assume everything was calculated correctly since VPS-5971 doesn't cover tracking segment numbers or program identifiers.
            var newMessages = new List<SCTEMessageBase>(baseMessages);
            newMessage.Messages = newMessages;

            schedule.InsertEvents(parentId, new ScheduledEvent[] { newMessage }, false, false, false);
            return newMessage;
        }

        private void SetEventPassthrough(ScheduledEvent evt)
        {
            bool resultPassThrough = false;
            var eventPassThrough = GetESPNProperty(evt, "SegmentPassThrough");
            if (String.IsNullOrEmpty(eventPassThrough))
            {
                eventPassThrough = GetESPNProperty(evt, "CommercialPassThrough");
            }
            bool passthrough;
            if (Boolean.TryParse(eventPassThrough, out passthrough) && passthrough)
            {
                resultPassThrough = true;
            }
            evt.CustomProperties.Add(CustomProperty.Keys.ESPNEventPassthrough, resultPassThrough);
        }

        private void AudioProfileSupport(ScheduledEvent evt)
        {
            var audioProfile = GetESPNProperty(evt, "AudioTemplate");
            if (String.IsNullOrEmpty(audioProfile)) return;

            evt.SwizzleProfileName = audioProfile;
        }

        private void TransitionSupport(ScheduledEvent evt)
        {
            var transition = GetESPNProperty(evt, "TransitionTypeIn");
            if (String.IsNullOrEmpty(transition)) return;

            switch (transition)
            {
                case "VFade":
                    evt.TransitionType = TransitionType.VFade;
                    break;
                case "TakeAndFade":
                    evt.TransitionType = TransitionType.CutFade;
                    break;
                case "FadeAndTake":
                    evt.TransitionType = TransitionType.FadeCut;
                    break;
                default:
                    break;
            }
        }

        private void NetworkIdentSupport(ScheduledEvent evt, ref string currentNetworkIdent)
        {
            var newIndent = GetESPNProperty(evt, "NetworkIdent");
            if (newIndent != null)
            {
                currentNetworkIdent = newIndent;
            }
            else
            {
                SetESPNProperty(evt, "NetworkIdent", currentNetworkIdent);
            }
        }

        private readonly HashSet<ScheduledEventType> c_BlacklistedTypes = new HashSet<ScheduledEventType>(
            new[] { ScheduledEventType.CommentEvent, ScheduledEventType.UnsupportedEvent, ScheduledEventType.VChipEvent });
        private void ClearUnnecessary(Schedule schedule, ref int i)
        {
            // Remove comment or other invalid event types from the schedule
            var evt = schedule.ScheduledEvents[i];
            if (c_BlacklistedTypes.Contains(evt.Type))
            {
                if (evt.ParentId != null)
                {
                    var parent = schedule.GetContainer(evt.ParentId.Value);
                    if (parent != null)
                    {
                        parent.RemoveChild(evt.UIdAsGuid);
                        parent.SecondaryIds.Remove(evt.UIdAsGuid);
                    }
                }
                if (evt.ParentId != null && evt.BasicEventDescriptor != BasicEventDescriptor.SecondaryEvent)
                {
                    var secondaries = schedule.ScheduledEvents.Where(ev => ev.SecondaryEventData != null && ev.SecondaryEventData.PrimaryEventId == evt.UId);
                    if (secondaries.Any())
                    {
                        var newPrimary = schedule.GetTopPrimaryOfContainer(evt.ParentId.Value);
                        if (newPrimary != null)
                        {
                            foreach (var secondaryEvent in secondaries.ToList())
                            {
                                secondaryEvent.SecondaryEventData.PrimaryEventId = newPrimary.UId;
                            }
                        }
                    }
                }
                schedule.ScheduledEvents.RemoveAt(i);
                i--;
                return;
            }

            // Remove unused extra translation info stored in custom properties
            ClearESPNProperties(evt);

            ClearBasicProperties(evt);
        }

        private static void ConsolidateAiringId(Schedule schedule)
        {
            foreach (var container in schedule.Containers.Values)
            {
                // This algorithm should strip away all lower level or non-program container's airing id
                // This will allow the SES to bubble up the container hierarchy to find the correct airing ID automatically
                if (container.ParentContainer == null || container.CustomProperties == null || container.CustomProperties.Count < 1) continue;

                // Check custom data field for AiringID
                object property;
                if (!container.CustomProperties.TryGetValue(CustomProperty.Keys.ESPNAiringID, out property)) continue;

                bool remove = false;
                if (container.ContainerType != FunctionalType.Program) remove = true;

                // Check parent's custom data field for AiringId
                if (!remove)
                {
                    for (var parentContainer = container; container.ParentContainer != null;)
                    {
                        parentContainer = schedule.Containers[parentContainer.ParentContainer.Value];

                        if (parentContainer.ContainerType != FunctionalType.Program || parentContainer.CustomProperties == null ||
                            parentContainer.CustomProperties.Count < 1) continue;

                        if (container.CustomProperties.TryGetValue(CustomProperty.Keys.ESPNAiringID, out property))
                        {
                            remove = true;
                            break;
                        }
                    }
                }
                if (remove)
                {
                    // Strip lower-level AiringId
                    container.CustomProperties.Remove(CustomProperty.Keys.ESPNAiringID);
                }
            }
        }

        private void ContainerSupport(Schedule schedule, Stack<ScheduleContainer> activeContainers, int i)
        {
            var evt = schedule.ScheduledEvents[i];

            if (evt.Type == ScheduledEventType.CommentEvent)
            {
                var type = evt.GetCustomPropertyOrDefault<string>("CommentType");
                if (type != null && String.Compare(type, "PrimaryProgramHeader", true) == 0 && evt.ParentId != null)
                {
                    var programContainer = schedule.GetContainer(evt.ParentId.Value);
                    if (programContainer != null)
                    {
                        var airingId = GetESPNProperty(evt, "AiringId");
                        programContainer.CustomProperties.Add(CustomProperty.Keys.ESPNAiringID, airingId);
                        var programPassThrough = GetESPNProperty(evt, "PgmPassThrough");
                        programContainer.CustomProperties.Add(CustomProperty.Keys.ESPNProgramPassthrough, programPassThrough);
                        if (programContainer.Name == null)
                        {
                            programContainer.Name = (string)evt.CustomProperties["CommentPrimaryProgramName"];
                        }
                        if (programContainer.Name == null)
                        {
                            programContainer.Name = (string)evt.CustomProperties["CommentPrimaryNonProgramName"];
                        }

                        int msOffset = GetRequestedPreroll(programContainer, ConvertTimecodeKind(schedule.Framerate));
                        if (msOffset > 0)
                        {
                            programContainer.CustomProperties.Add(CustomProperty.Keys.ESPNSCTEPreroll, msOffset.ToString());
                        }
                    }
                }
            }

            if (evt.ParentId != null)
            {
                var parentContainer = schedule.GetContainer(evt.ParentId.Value);
                if (parentContainer != null && parentContainer.ContainerType == FunctionalType.Commercial && parentContainer.CType == ContainerType.BreakPod &&
                    parentContainer.Name != null && parentContainer.Name.StartsWith(c_bonusMarker))
                {
					// ESPN wanted to disable the bonus pod
                    //parentContainer.CType = ContainerType.BonusPod;
                }

                if (parentContainer != null && !parentContainer.CustomProperties.ContainsKey(CustomProperty.Keys.ESPNSCTEPreroll))
                {
                    int msOffset = GetRequestedPreroll(parentContainer, ConvertTimecodeKind(schedule.Framerate));
                    if (msOffset > 0)
                    {
                        parentContainer.CustomProperties.Add(CustomProperty.Keys.ESPNSCTEPreroll, msOffset.ToString());
                    }
                }
            }

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

        public static TimecodeKind ConvertTimecodeKind(double frameRate)
        {
            var kind = TimecodeKind.Unspecified;
            switch ((int)Math.Ceiling(frameRate))
            {
                case 24:
                    kind = TimecodeKind.Smpte24;
                    break;
                case 25:
                    kind = TimecodeKind.Smpte25;
                    break;
                case 0:  // Make 30 non drop frame the default because I don't know what else to do.  Need to be able to get SES timecode via (the appropriate) TimecodeService?
                case 30:
                    var isDropFrame = Math.Ceiling(frameRate) != frameRate;
                    kind = isDropFrame ? TimecodeKind.Smpte30d : TimecodeKind.Smpte30;
                    break;
                case 50:
                    kind = TimecodeKind.Smpte50;
                    break;
                case 60:
                    isDropFrame = Math.Ceiling(frameRate) != frameRate;
                    kind = isDropFrame ? TimecodeKind.Smpte60d : TimecodeKind.Smpte60;
                    break;
                default:
                    kind = TimecodeKind.Other;
                    break;
            }

            return kind;
        }

        private string GetESPNProperty(ScheduledEvent header, string propertyName)
        {
            return GetESPNProperty(propertyName, header.CustomProperties);
        }

        private string GetESPNProperty(ScheduleContainer header, string propertyName)
        {
            return GetESPNProperty(propertyName, header.CustomProperties);
        }

        private static string GetESPNProperty(string propertyName, Dictionary<string, object> customProperties)
        {
            var basePath = new BXFPrivateInfoBasePath(BXFPrivateInfoBase.EventData);
            for (int branch = 1; true; branch++)
            {
                var path = new[] { new BXFPrivateInfoPathStep("Imagine"), new BXFPrivateInfoPathStep("Parameter", branch) };
                var propName = customProperties.GetBXFPrivateInfo(basePath, path, "name");
                if (propName == null) return null;

                if (String.Compare(propName.Item2 as string, propertyName, true) == 0)
                {
                    var propValue = customProperties.GetBXFPrivateInfo(basePath, path, "value");
                    if (propValue == null) return null;

                    return propValue.Item2 as string;
                }
            }
        }

        private void SetESPNProperty(ScheduledEvent header, string propertyName, string propertyValue)
        {
            var basePath = new BXFPrivateInfoBasePath(BXFPrivateInfoBase.EventData);
            for (int branch = 1; true; branch++)
            {
                var path = new[] { new BXFPrivateInfoPathStep("Imagine"), new BXFPrivateInfoPathStep("Parameter", branch) };
                var propName = header.CustomProperties.GetBXFPrivateInfo(basePath, path, "name");
                if (propName != null) continue;

                header.CustomProperties.Add(BXFCustomPropertyManipulator.BuildPath(basePath, path, "name"), propertyName);
                header.CustomProperties.Add(BXFCustomPropertyManipulator.BuildPath(basePath, path, "value"), propertyValue);
                return;
            }
        }

        private void ClearESPNProperties(ScheduledEvent evt)
        {
            // Keep some around for the as runs
            bool any = true;
            for (int branch = 1; any; branch++)
            {
                any = false;
                Tuple<string, object> propValue;
                var propName = evt.CustomProperties.GetBXFPrivateInfo(new BXFPrivateInfoBasePath(BXFPrivateInfoBase.EventData),
                    new[] { new BXFPrivateInfoPathStep("Imagine"), new BXFPrivateInfoPathStep("Parameter", branch) }, "name");
                if (propName != null)
                {
                    any = true;
                    if (propName.Item2.ToString() != "NetworkIdent")
                    {
                        evt.CustomProperties.Remove(propName.Item1);

                        propValue = evt.CustomProperties.GetBXFPrivateInfo(new BXFPrivateInfoBasePath(BXFPrivateInfoBase.EventData),
                            new[] { new BXFPrivateInfoPathStep("Imagine"), new BXFPrivateInfoPathStep("Parameter", branch) }, "value");
                        if (propValue != null)
                        {
                            evt.CustomProperties.Remove(propValue.Item1);
                        }
                    }
                }

                propName = evt.CustomProperties.GetBXFPrivateInfo(new BXFPrivateInfoBasePath(BXFPrivateInfoBase.EventData),
                    new[] { new BXFPrivateInfoPathStep("ImagineCommunications", branch), new BXFPrivateInfoPathStep("FormerSpotType") });
                if (propName != null)
                {
                    any = true;
                    evt.CustomProperties.Remove(propName.Item1);
                }
            }
        }

        private void ClearBasicProperties(ScheduledEvent evt)
        {
            // Probably more but I'll leave it at that.
            evt.CustomProperties.Remove(CustomProperty.Keys.RouterSource);
        }

        private void SupportLocalBreakContainer(Schedule schedule, int i, Stack<ScheduleContainer> activeContainers)
        {
            // Make sure that a local conatiner is only added once
            if (schedule.ScheduledEvents[i].BasicEventDescriptor != BasicEventDescriptor.PrimaryEvent) return;

            if (activeContainers != null && activeContainers.Count > 0 && schedule.ScheduledEvents[i].ParentId != activeContainers.Peek().ParentContainer)
            {
                activeContainers.Pop();
            }
            // This assumes that secondary events of a priamry will be in the schedule between the primary event and the next primary event
            for (var j = 1; i + j < schedule.ScheduledEvents.Count && schedule.ScheduledEvents[i + j].BasicEventDescriptor != BasicEventDescriptor.PrimaryEvent; j++)
            {
                var nextEvent = schedule.ScheduledEvents[i + j] as UnsupportedEvent;
                // A secondary for the same primary
                if (nextEvent == null) continue;
                if (nextEvent.SecondaryEventData == null || nextEvent.SecondaryEventData.PrimaryEventId != schedule.ScheduledEvents[i].UId) return;

                var typeName = nextEvent.OriginalTypeName;
                if (typeName == "AFFILIATE STOP")
                {
                    // Drop back to network
                    if (activeContainers.Count > 0)
                    {
                        activeContainers.Pop();
                    }
                }
                else if (typeName == "AFFILIATE SENT")
                {
                    ScheduleContainer networkContainer = null;
                    var primary = schedule.ScheduledEvents[i];
                    if (primary != null && primary.ParentId != null)
                    {
                        networkContainer = schedule.GetContainer(primary.ParentId.Value);
                    }
                    // Push forward to local break
                    var localContainer = new ScheduleContainer()
                    {
                        ContainerType = FunctionalType.Commercial,
                        Name = networkContainer != null ? networkContainer.Name + " Local" : "Local",
                        CType = ContainerType.Normal,
                        UId = nextEvent.UIdAsGuid  // The secondary event will not stay in the schedule so reuse it's id
                    };

                    int msOffset = GetRequestedPreroll(nextEvent, ConvertTimecodeKind(schedule.Framerate));
                    if (msOffset > 0)
                    {
                        localContainer.CustomProperties.Add(CustomProperty.Keys.ESPNSCTEPreroll, msOffset.ToString());
                    }

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
    }
}