using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
    public class GOAPNode
    {
        public GOAPNode? Parent;
        public float RunningCost;
        public IPrimitiveTask Task;

        /// <summary>
        /// Default constructor.
        /// Initializes Task to EmptyTask.Instance to prevent null reference issues.
        /// Parent is null by default, suitable for a root node or unlinked node.
        /// RunningCost is 0f by default.
        /// </summary>
        public GOAPNode()
        {
            Parent = null; // Explicitly null for clarity, though default for reference types
            RunningCost = 0f;
            Task = EmptyTask.Instance; // Default to EmptyTask
        }

        // Optional: Constructor for easier initialization if needed elsewhere
        public GOAPNode(GOAPNode? parent, float runningCost, IPrimitiveTask task)
        {
            Parent = parent;
            RunningCost = runningCost;
            Task = task ?? EmptyTask.Instance; // Ensure task is never null
        }
    }
}
