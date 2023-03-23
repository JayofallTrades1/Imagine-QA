--
-- PostgreSQL database dump
--

-- Dumped from database version 9.6.20
-- Dumped by pg_dump version 9.6.20

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--ALTER TABLE ONLY public.thumbnails DROP CONSTRAINT thumbnails_pkey;
--ALTER TABLE ONLY public.profiles DROP CONSTRAINT profiles_pkey;
--ALTER TABLE ONLY public.profiles DROP CONSTRAINT profiles_orderindex_key;
--ALTER TABLE ONLY public.profiles DROP CONSTRAINT profiles_key_key;
--ALTER TABLE ONLY public.layouts DROP CONSTRAINT layouts_pkey;
--ALTER TABLE ONLY public.layouts DROP CONSTRAINT layouts_orderindex_key;
--ALTER TABLE ONLY public.layouts DROP CONSTRAINT layouts_key_key;
--ALTER TABLE public.profiles ALTER COLUMN orderindex DROP DEFAULT;
--ALTER TABLE public.profiles ALTER COLUMN id DROP DEFAULT;
--ALTER TABLE public.layouts ALTER COLUMN orderindex DROP DEFAULT;
--ALTER TABLE public.layouts ALTER COLUMN id DROP DEFAULT;
--DROP TABLE public.thumbnails;
--DROP SEQUENCE public.profiles_orderindex_seq;
--DROP SEQUENCE public.profiles_id_seq;
--DROP TABLE public.profiles;
--DROP SEQUENCE public.layouts_orderindex_seq;
--DROP SEQUENCE public.layouts_id_seq;
--DROP TABLE public.layouts;
--DROP EXTENSION plpgsql;
--DROP SCHEMA public;
--
-- Name: public; Type: SCHEMA; Schema: -; Owner: postgres
--
--CREATE SCHEMA public;


ALTER SCHEMA public OWNER TO postgres;

--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: postgres
--

COMMENT ON SCHEMA public IS 'standard public schema';


--
-- Name: plpgsql; Type: EXTENSION; Schema: -; Owner: 
--

CREATE EXTENSION IF NOT EXISTS plpgsql WITH SCHEMA pg_catalog;


--
-- Name: EXTENSION plpgsql; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION plpgsql IS 'PL/pgSQL procedural language';


SET default_tablespace = '';

SET default_with_oids = false;

--
-- Name: layouts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.layouts (
    id bigint NOT NULL,
    orderindex bigint NOT NULL,
    key character varying(256),
    data jsonb
);


ALTER TABLE public.layouts OWNER TO postgres;

--
-- Name: layouts_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.layouts_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.layouts_id_seq OWNER TO postgres;

--
-- Name: layouts_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.layouts_id_seq OWNED BY public.layouts.id;


--
-- Name: layouts_orderindex_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.layouts_orderindex_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.layouts_orderindex_seq OWNER TO postgres;

--
-- Name: layouts_orderindex_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.layouts_orderindex_seq OWNED BY public.layouts.orderindex;


--
-- Name: profiles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.profiles (
    id bigint NOT NULL,
    orderindex bigint NOT NULL,
    key character varying(256),
    data jsonb
);


ALTER TABLE public.profiles OWNER TO postgres;

--
-- Name: profiles_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.profiles_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.profiles_id_seq OWNER TO postgres;

--
-- Name: profiles_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.profiles_id_seq OWNED BY public.profiles.id;


--
-- Name: profiles_orderindex_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.profiles_orderindex_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.profiles_orderindex_seq OWNER TO postgres;

--
-- Name: profiles_orderindex_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.profiles_orderindex_seq OWNED BY public.profiles.orderindex;


--
-- Name: thumbnails; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.thumbnails (
    key character varying NOT NULL,
    mimetype character varying NOT NULL,
    height integer NOT NULL,
    width integer NOT NULL,
    data bytea NOT NULL
);


ALTER TABLE public.thumbnails OWNER TO postgres;

--
-- Name: layouts id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layouts ALTER COLUMN id SET DEFAULT nextval('public.layouts_id_seq'::regclass);


--
-- Name: layouts orderindex; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layouts ALTER COLUMN orderindex SET DEFAULT nextval('public.layouts_orderindex_seq'::regclass);


--
-- Name: profiles id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.profiles ALTER COLUMN id SET DEFAULT nextval('public.profiles_id_seq'::regclass);


--
-- Name: profiles orderindex; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.profiles ALTER COLUMN orderindex SET DEFAULT nextval('public.profiles_orderindex_seq'::regclass);


--
-- Data for Name: layouts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.layouts (id, orderindex, key, data) FROM stdin;
1	1	Master Control	{"name": "MasterControl", "roles": [], "tiles": [{"x": 1, "y": 0, "widgetInstance": {"id": "w_ce3b9669-14d6-49ff-934c-1fdcc3920742", "data": {"tally": {"mode": "Full", "useParent": false, "showFrames": false}, "target": {"targetId": "", "useParent": true}, "automation": {"channelId": {"id": "", "name": ""}, "useParent": false, "showChannelName": false}}, "name": "AutomationTallyWidget", "size": {"name": "Full", "width": 9, "height": 1}, "sizeConstraint": {"minWidth": 3, "maxHeight": 1, "minHeight": 1}}}, {"x": 1, "y": 1, "widgetInstance": {"id": "w_6c77874c-14f0-47ad-af79-b4166dce498c", "data": {"target": {"targetId": "", "useParent": true}, "switcherbus": {"useParent": false, "numSources": "15", "automaticNumSources": false}}, "name": "SwitcherBusWidget", "size": {"name": "Custom", "width": 18, "height": 3}, "sizeConstraint": {"minWidth": 3, "minHeight": 3}}}, {"x": 1, "y": 4, "widgetInstance": {"id": "w_be184adb-aad5-40d0-82e1-808ae4e6b328", "data": {"target": {"targetId": "", "useParent": true}, "preview": {"useParent": false, "outputStream": "program", "showVuMeters": false, "numVuMetersShown": 1}}, "name": "PreviewWidget", "size": {"name": "Custom", "width": 4, "height": 3}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 1, "y": 7, "widgetInstance": {"id": "w_5b8209d1-8f8e-46a2-8d30-10555e549bd8", "data": {"target": {"targetId": "", "useParent": true}, "preview": {"useParent": false, "outputStream": "preview", "showVuMeters": false, "numVuMetersShown": 1}}, "name": "PreviewWidget", "size": {"name": "Custom", "width": 4, "height": 3}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 5, "y": 4, "widgetInstance": {"id": "w_08b20bb2-f7a7-40c8-b1f9-6ce3767e7010", "data": {"target": {"targetId": "", "useParent": true}, "transitionbus": {"useParent": false}}, "name": "TransitionBusWidget", "size": {"name": "Normal", "width": 10, "height": 1}, "sizeConstraint": {"minWidth": 7, "maxHeight": 2, "minHeight": 1}}}, {"x": 5, "y": 5, "widgetInstance": {"id": "w_8197fc85-3631-46bd-ba81-25c5ebef6b8e", "data": {"target": {"targetId": "", "useParent": true}, "layerbus": {"numSalvos": 3, "useParent": false, "selectMode": true, "orientation": "Right", "showKillAll": true, "layerNumbers": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], "showClearPreview": true, "showClearProgram": true}}, "name": "LayerBusWidget", "size": {"name": "Custom", "width": 14, "height": 4}, "sizeConstraint": {"minWidth": 3, "minHeight": 3}}}, {"x": 11, "y": 0, "widgetInstance": {"id": "w_5ea6edd4-c135-488c-8de2-cd7e13b448f8", "data": {"target": {"targetId": "", "useParent": true}, "appearance": {"color": "rgba(255, 255, 255)", "isBold": false, "fontScale": 1.5, "useParent": false, "background": "rgba(0, 0, 0, 0)", "defaultFontSize": 2.3}, "countdownclock": {"type": "Channel Time", "channelId": null, "showLabel": true, "useParent": false, "showFrames": false, "orientation": "Center", "channelOverride": false}}, "name": "CountdownClockWidget", "size": {"name": "Normal", "width": 3, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 12, "y": 9, "widgetInstance": {"id": "w_46e2f907-b42b-43e1-8b6a-8f83cb84a407", "data": {"target": {"targetId": "", "useParent": true}, "controls": {"useParent": false, "stopLocked": true}, "automation": {"channelId": {"id": "", "name": ""}, "useParent": false, "showChannelName": false}}, "name": "ControlsWidget", "size": {"name": "All", "width": 3, "height": 1}, "sizeConstraint": {"maxWidth": 3, "minWidth": 3, "maxHeight": 1, "minHeight": 1}}}, {"x": 15, "y": 0, "widgetInstance": {"id": "w_a29b7eff-f7e5-4bcf-8396-5011695aabeb", "data": {"target": {"targetId": "", "useParent": true}, "targetselect": {"useParent": true}}, "name": "TargetSelectWidget", "size": {"name": "Normal", "width": 4, "height": 1}, "sizeConstraint": {"minWidth": 3, "maxHeight": 1, "minHeight": 1}}}, {"x": 15, "y": 9, "widgetInstance": {"id": "w_50b6ee8c-d80b-45e6-8013-d78ae390c01d", "data": {"target": {"targetId": "", "useParent": true}, "takenext": {"useParent": false, "alwaysEnable": false}, "appearance": {"color": "#ffffff", "isBold": true, "fontScale": 1.7, "useParent": false, "background": "#66d", "defaultFontSize": 1.5}, "automation": {"channelId": {"id": "", "name": ""}, "useParent": false, "showChannelName": false}}, "name": "TakeNextButtonWidget", "size": {"name": "Medium", "width": 2, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 17, "y": 9, "widgetInstance": {"id": "w_ba56a362-7d96-4985-b927-981b424f343f", "data": {"target": {"targetId": "", "useParent": true}, "appearance": {"color": "#ffffff", "isBold": true, "fontScale": 1.7, "useParent": false, "background": "#66d", "defaultFontSize": 1.5}, "configurablebutton": {"actions": ["Take Salvos on Program", "Take Selected Graph Buttons", "Take Switcher Bus"], "useParent": false}}, "name": "ConfigurableButtonWidget", "size": {"name": "Medium", "width": 2, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 19, "y": 2, "widgetInstance": {"id": "w_45326c6f-020f-4c00-ac49-9599f1577261", "data": {"target": {"targetId": "", "useParent": true}, "layerpanel": {"useParent": false, "currentTab": 0, "showKillAll": false, "showClearPreview": true, "showClearProgram": true}}, "name": "LayerPanelWidget", "size": {"name": "Custom", "width": 4, "height": 8}, "sizeConstraint": {"minWidth": 3, "minHeight": 6}}}], "width": "23", "config": {"target": {"targetId": "5dad7e5d-660e-4ff6-8c08-c9283c33c757"}}, "height": 10, "globals": {}, "lineageId": "l_4991400e-0675-4ec7-a4d7-117b3eeb2223", "containers": []}
2	2	MC Playlist	{"name": "MC Playlist", "roles": [], "tiles": [{"x": 0, "y": 0, "widgetInstance": {"id": "w_df3165ec-c70c-4001-baed-6d5010aca359", "data": {"target": {"targetId": "", "useParent": true}, "targetselect": {"useParent": true}}, "name": "TargetSelectWidget", "size": {"name": "Normal", "width": 4, "height": 1}, "sizeConstraint": {"minWidth": 3, "maxHeight": 1, "minHeight": 1}}}, {"x": 0, "y": 1, "widgetInstance": {"id": "w_b70032af-94c4-41e8-aa21-6a92e7e54e8e", "data": {"target": {"targetId": "", "useParent": true}, "switcherbus": {"useParent": false, "numSources": 8, "automaticNumSources": false}}, "name": "SwitcherBusWidget", "size": {"name": "Custom", "width": 13, "height": 3}, "sizeConstraint": {"minWidth": 3, "minHeight": 3}}}, {"x": 0, "y": 4, "widgetInstance": {"id": "w_eb311dc1-ef2b-4372-bce9-6f4da37b0fed", "data": {"target": {"targetId": "", "useParent": true}, "layerpanel": {"useParent": false, "currentTab": 0, "showKillAll": false, "showClearPreview": true, "showClearProgram": true}}, "name": "LayerPanelWidget", "size": {"name": "4x6", "width": 4, "height": 6}, "sizeConstraint": {"minWidth": 3, "minHeight": 6}}}, {"x": 4, "y": 0, "widgetInstance": {"id": "w_48d542ad-a3ac-4ca5-aee8-411fe3b36d48", "data": {"tally": {"mode": "Full", "useParent": false, "showFrames": false}, "target": {"targetId": "", "useParent": true}, "automation": {"channelId": {"id": "", "name": ""}, "useParent": false, "showChannelName": false}}, "name": "AutomationTallyWidget", "size": {"name": "Full", "width": 9, "height": 1}, "sizeConstraint": {"minWidth": 3, "maxHeight": 1, "minHeight": 1}}}, {"x": 4, "y": 4, "widgetInstance": {"id": "w_714130d9-6423-4c4d-b86d-5b48fd6af07b", "data": {"target": {"targetId": "", "useParent": true}, "layerbus": {"numSalvos": 3, "useParent": false, "selectMode": true, "orientation": "Right", "showKillAll": false, "layerNumbers": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], "showClearPreview": false, "showClearProgram": false}}, "name": "LayerBusWidget", "size": {"name": "Custom", "width": 9, "height": 4}, "sizeConstraint": {"minWidth": 3, "minHeight": 3}}}, {"x": 4, "y": 8, "widgetInstance": {"id": "w_fc632b2e-a5bf-4f33-8cbb-51bb10a01380", "data": {"target": {"targetId": "5ce72986-c4af-45fa-a216-cf22915933e2", "useParent": true}, "preview": {"useParent": false, "outputStream": "preview", "showVuMeters": true, "numVuMetersShown": "2"}}, "name": "PreviewWidget", "size": {"name": "Custom", "width": 3, "height": 2}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 7, "y": 8, "widgetInstance": {"id": "w_56e269c9-d473-4495-a132-c82cac1446ef", "data": {"target": {"targetId": "5ce72986-c4af-45fa-a216-cf22915933e2", "useParent": true}, "preview": {"useParent": false, "outputStream": "program", "showVuMeters": true, "numVuMetersShown": "2"}}, "name": "PreviewWidget", "size": {"name": "Custom", "width": 3, "height": 2}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 11, "y": 9, "widgetInstance": {"id": "w_14f1e3ae-62a7-4fe2-8e93-818b6945e7dc", "data": {"target": {"targetId": "", "useParent": true}, "appearance": {"color": "#ffffff", "isBold": true, "fontScale": 1.7, "useParent": false, "background": "#66d", "defaultFontSize": 1.5}, "configurablebutton": {"actions": ["Take Salvos on Program", "Take Selected Graph Buttons", "Take Switcher Bus"], "useParent": false}}, "name": "ConfigurableButtonWidget", "size": {"name": "Custom", "width": 3, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 13, "y": 0, "widgetInstance": {"id": "w_72372699-40be-4a82-8ed8-545c6fe857e9", "data": {"target": {"targetId": "", "useParent": true}, "appearance": {"color": "rgba(255, 255, 255)", "isBold": false, "fontScale": 1.5, "useParent": false, "background": "rgba(0, 0, 0, 0)", "defaultFontSize": 2.3}, "countdownclock": {"type": "Channel Time", "channelId": null, "showLabel": true, "useParent": false, "showFrames": false, "orientation": "Center", "channelOverride": false}}, "name": "CountdownClockWidget", "size": {"name": "Normal", "width": 3, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 13, "y": 1, "widgetInstance": {"id": "w_d6d09532-5141-4e60-bcf2-a62c4ff6c864", "data": {"target": {"targetId": "5ce72986-c4af-45fa-a216-cf22915933e2", "useParent": true}, "playlist": {"test": "thing", "flash": false, "columns": [{"id": "startTime", "name": "Time", "selected": true}, {"id": "duration", "name": "Dur/Rem", "selected": true}, {"id": "som", "name": "SOM", "selected": true}, {"id": "eom", "name": "EOM", "selected": false}, {"id": "source", "name": "Source", "selected": true}, {"id": "houseId", "name": "HouseID", "selected": true}, {"id": "title", "name": "Title", "selected": true}, {"id": "status", "name": "Status", "selected": true}, {"id": "functionalType", "name": "Functional Type", "selected": true}, {"id": "startMode", "name": "Start Mode", "selected": true}, {"id": "rate", "name": "Rate", "selected": true}, {"id": "progress", "name": "Progress", "selected": true}, {"id": "transition", "name": "Trans", "selected": true}, {"id": "extras", "name": "Extras", "selected": true}], "useParent": false, "numPinnedEvents": "2"}}, "name": "PlaylistWidget", "size": {"name": "Custom", "width": 10, "height": 8}, "sizeConstraint": {"minWidth": 4, "minHeight": 4}}}, {"x": 14, "y": 9, "widgetInstance": {"id": "w_3edda20d-d326-4cac-b1d5-9d19ffb1a14a", "data": {"target": {"targetId": "", "useParent": true}, "takenext": {"useParent": false, "alwaysEnable": false}, "appearance": {"color": "#ffffff", "isBold": true, "fontScale": 1.7, "useParent": false, "background": "#66d", "defaultFontSize": 1.5}, "automation": {"channelId": {"id": "", "name": ""}, "useParent": false, "showChannelName": false}}, "name": "TakeNextButtonWidget", "size": {"name": "Medium", "width": 2, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 16, "y": 0, "widgetInstance": {"id": "w_2e6f6ce1-afd5-44cf-8fcd-d6c9eb4dcff0", "data": {"target": {"targetId": "5ce72986-c4af-45fa-a216-cf22915933e2", "useParent": true}, "appearance": {"color": "rgba(255, 255, 255)", "isBold": false, "fontScale": 1.5, "useParent": false, "background": "rgba(0, 0, 0, 0)", "defaultFontSize": 2.3}, "countdownclock": {"type": "Time Remaining", "channelId": null, "showLabel": true, "useParent": false, "showFrames": false, "orientation": "Center", "channelOverride": false}}, "name": "CountdownClockWidget", "size": {"name": "Normal", "width": 3, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}, {"x": 19, "y": 0, "widgetInstance": {"id": "w_f150a474-1e5c-4308-8778-765fe4f47253", "data": {"target": {"targetId": "5ce72986-c4af-45fa-a216-cf22915933e2", "useParent": true}, "appearance": {"color": "rgba(255,25,0,1)", "isBold": false, "fontScale": 1.5, "useParent": false, "background": "rgba(0, 0, 0, 0)", "defaultFontSize": 2.3}, "countdownclock": {"type": "Break Duration", "channelId": null, "showLabel": true, "useParent": false, "showFrames": false, "orientation": "Center", "channelOverride": false}}, "name": "CountdownClockWidget", "size": {"name": "Normal", "width": 3, "height": 1}, "sizeConstraint": {"minWidth": 1, "minHeight": 1}}}], "width": "23", "config": {"target": {"targetId": "5ce72986-c4af-45fa-a216-cf22915933e2"}}, "height": 10, "globals": {}, "lineageId": "l_cd395148-da57-4511-a0b5-3289cf55d370", "containers": []}
\.


--
-- Name: layouts_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.layouts_id_seq', 1, true);


--
-- Name: layouts_orderindex_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.layouts_orderindex_seq', 2, true);


--
-- Data for Name: profiles; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.profiles (id, orderindex, key, data) FROM stdin;
1	1	mknight	{"pageSize": 3, "username": "mknight", "favouriteLayouts": ["Master Control", "MC Playlist"]}
\.


--
-- Name: profiles_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.profiles_id_seq', 1, true);


--
-- Name: profiles_orderindex_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.profiles_orderindex_seq', 1, true);


--
-- Data for Name: thumbnails; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.thumbnails (key, mimetype, height, width, data) FROM stdin;
\.


--
-- Name: layouts layouts_key_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layouts
    ADD CONSTRAINT layouts_key_key UNIQUE (key);


--
-- Name: layouts layouts_orderindex_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layouts
    ADD CONSTRAINT layouts_orderindex_key UNIQUE (orderindex) DEFERRABLE;


--
-- Name: layouts layouts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layouts
    ADD CONSTRAINT layouts_pkey PRIMARY KEY (id);


--
-- Name: profiles profiles_key_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.profiles
    ADD CONSTRAINT profiles_key_key UNIQUE (key);


--
-- Name: profiles profiles_orderindex_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.profiles
    ADD CONSTRAINT profiles_orderindex_key UNIQUE (orderindex) DEFERRABLE;


--
-- Name: profiles profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.profiles
    ADD CONSTRAINT profiles_pkey PRIMARY KEY (id);


--
-- Name: thumbnails thumbnails_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.thumbnails
    ADD CONSTRAINT thumbnails_pkey PRIMARY KEY (key);


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: postgres
--

GRANT ALL ON SCHEMA public TO PUBLIC;


--
-- PostgreSQL database dump complete
--

