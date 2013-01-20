

namespace Dema.BlenX.Parser
{
   public enum TempType
   {
      TEMP_PPROC = 10,
      TEMP_TYPE = 11,
      TEMP_NAME = 12,
      TEMP_SEQ = 13,
      TEMP_RATE = 14
   }

   // Entity types
   public enum EntityType
   {
      PPROC,
      BPROC,
      MOL,
      SEQUENCE,
      STATE_VAR,
      EVENT_DEF,
      PPROC_TEMPLATE,
      BPROC_TEMPLATE,
      PPROC_TEMPLATE_REF = 10,
      TYPE_TEMPLATE_REF = 11,
      NAME_TEMPLATE_REF = 12,
      SEQ_TEMPLATE_REF = 13,
      RATE_TEMPLATE_REF = 14
   }

   public enum ActionType
   {
      INPUT,
      OUTPUT,
      HIDE,
      UNHIDE
   }

   public enum PiProcessType
   {
      PP_ROOT,
      PP_NIL,
      PP_ID,
      PP_SEQ,
      PP_REPLICATION,
      PP_PARALLEL,
      PP_CHOICE,
      PP_TAU,
      PP_INPUT,
      PP_OUTPUT,
      PP_EXPOSE,
      PP_HIDE,
      PP_UNHIDE
   }

   public enum BinderState
   {
      STATE_NOT_SPECIFIED,
      STATE_HIDDEN,
      STATE_BOUND,
      STATE_UNHIDDEN
   }

   public enum CondType
   {
      COND_RATE,
      COND_RATE_NORMAL,
      COND_RATE_GAMMA,
      COND_RATE_HYPEXP,
      COND_COUNT_EQUAL,
      COND_COUNT_GREATER,
      COND_COUNT_LESS,
      COND_COUNT_NEQUAL,
      COND_COUNT_CHANGED,
      COND_TIME_EQUAL,
      COND_STEP_EQUAL,
      COND_RATE_FUN,
      COND_STATELIST,
      COND_EXPRESSION,
      COND_RATE_IMMEDIATE,
      COND_STATES
   }

   public enum VerbType
   {
      VERB_NOTHING = 0,
      VERB_SPLIT,
      VERB_NEW,
      VERB_DELETE,
      VERB_JOIN,
      VERB_UPDATE
   }

   public enum StateType
   {
      STATE_UP,
      STATE_DOWN
   }
}
