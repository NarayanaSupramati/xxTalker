-- Type: message_type

-- DROP TYPE IF EXISTS public."message_type";

CREATE TYPE public."message_type" AS ENUM
    ('message', 'rating', 'info');

ALTER TYPE public."message_type"
    OWNER TO xxtalker;
	
	
------------------------------------------
-- Table: public.talker_message

-- DROP TABLE IF EXISTS public."talker_message";

CREATE TABLE IF NOT EXISTS public."talker_message"
(
	message_id serial NOT NULL,
    receiver_account character varying(48) COLLATE pg_catalog."default" NOT NULL,
    sender_account character varying(48) COLLATE pg_catalog."default",
    receiver_identity character varying(32) COLLATE pg_catalog."default",
    sender_identity character varying(32) COLLATE pg_catalog."default",
    receiver_role character varying(50) COLLATE pg_catalog."default",
    sender_role character varying(50) COLLATE pg_catalog."default",
    signature character varying(130) COLLATE pg_catalog."default" NOT NULL,
    message character varying(1000) COLLATE pg_catalog."default",
    message_num integer NOT NULL DEFAULT 0,
    message_date timestamp with time zone NOT NULL DEFAULT now(),
    message_type message_type NOT NULL DEFAULT 'message'::message_type,
    ref_message_num integer,
    rating integer NOT NULL DEFAULT 0,
    CONSTRAINT talker_message_pkey PRIMARY KEY (message_id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.talker_message
    OWNER to xxtalker;


------------------------------------------
-- View: public.talker

-- DROP VIEW public.talker;

CREATE OR REPLACE VIEW public.talker
 AS
 SELECT t1.to_account AS account_id,
    t2.message AS info,
    btrim(to_char(avg(t1.rating), '0.99'::text)) AS rating
   FROM ( SELECT m.to_account,
            m.rating
           FROM talker_message m,
            ( SELECT talker_message.to_account,
                    talker_message.from_account,
                    max(talker_message.message_num) AS num
                   FROM talker_message
                  WHERE talker_message.message_type = 'rating'::message_type
                  GROUP BY talker_message.to_account, talker_message.from_account) last_rating
          WHERE m.to_account::text = last_rating.to_account::text AND m.message_num = last_rating.num) t1
     LEFT JOIN ( SELECT m.to_account,
            m.message
           FROM talker_message m,
            ( SELECT talker_message.to_account,
                    max(talker_message.message_num) AS num
                   FROM talker_message
                  WHERE talker_message.message_type = 'info'::message_type
                  GROUP BY talker_message.to_account) last_info
          WHERE m.to_account::text = last_info.to_account::text AND m.message_num = last_info.num) t2 ON t2.to_account::text = t1.to_account::text
  GROUP BY t1.to_account, t2.message;

ALTER TABLE public.talker
    OWNER TO xxtalker;

