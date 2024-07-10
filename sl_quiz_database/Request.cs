namespace sl_quiz_database
{

    public enum Command
    {
        Create,
        Update,
        Delete,
        Retrieve,
    }

    public class Request
    {
        public Command Command = Command.Retrieve;
        public Question? question = null;    
    }
}