namespace ArcenXE.Utilities
{
    public class UndoRedo
    {
        public static readonly Stack<DoAction> Undo = new Stack<DoAction>();
        public static readonly Stack<DoAction> Redo = new Stack<DoAction>();

        public delegate void DoAction( ActionType action ); //contains the action to be done when it gets called (write text, create control, node, delete, etc.)

        public enum ActionType
        {
            Execute,
            Undo,
            Redo
        }
    }
}
