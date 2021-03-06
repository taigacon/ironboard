﻿namespace IronBoard.Vsix
{
   interface IGlobalPanel
   {
      void UpdateState();
   }

   public enum GlobalState
   {
      NoSolutionOpen,
      NoConfigFile,
      //NotUnderSourceControl,
      Operational
   }

   static class Extension
   {
      public static IGlobalPanel Panel { get; set; }

      private static GlobalState _state = GlobalState.NoSolutionOpen;
      public static GlobalState State
      {
         get { return _state;  }
         set
         {
            _state = value;
            if(Panel != null) Panel.UpdateState();
         }
      }
   }
}
