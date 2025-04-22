using System.Collections.ObjectModel;

namespace WinSystemCtl.Core.Data
{
    /// <summary>
    /// Overall class for _task item
    /// </summary>
    public class Task : ViewModelBase
    {
        private string? name;

        /// <summary>
        /// Name of this _task item
        /// </summary>
        public string? Name
        {
            get => name;
            set => Set(ref name, value);
        }

        private string? description;

        /// <summary>
        /// Description of this _task item
        /// </summary>
        public string? Description
        {
            get => description;
            set => Set(ref description, value);
        }

        private ObservableCollection<SingleStep>? preprocessSteps;

        /// <summary>
        /// List of steps before executed
        /// </summary>
        public ObservableCollection<SingleStep>? PreprocessSteps
        {
            get => preprocessSteps;
            set => Set(ref preprocessSteps, value);
        }

        private SingleStep? target;

        /// <summary>
        /// Target of this _task to be executed
        /// </summary>
        public SingleStep? Target
        {
            get => target;
            set => Set(ref target, value);
        }

        private string? encoding;

        /// <summary>
        /// Name of the _encoding to be used, during all execution.
        /// If null, use UTF8 as default.
        /// </summary>
        public string? Encoding
        {
            get => encoding;
            set => Set(ref encoding, value);
        }

        private ObservableCollection<SingleStep>? postprocessSteps;

        /// <summary>
        /// List of steps after executed
        /// </summary>
        public ObservableCollection<SingleStep>? PostprocessSteps
        {
            get => postprocessSteps;
            set => Set(ref postprocessSteps, value);
        }

        public void UpdateInplace(Task task)
        {
            if (task == null) { return; }

            Name = task.Name;
            Description = task.Description;
            Encoding = task.Encoding;
            if (task.PreprocessSteps != null && task.PreprocessSteps.Count > 0)
            {
                foreach (var step in task.PreprocessSteps)
                {
                    var newStep = new SingleStep();
                    newStep.UpdateInplace(step);
                    PreprocessSteps?.Add(newStep);
                }
            }
            if (task.Target != null)
            {
                var newStep = new SingleStep();
                newStep.UpdateInplace(task.Target);
                Target = newStep;
            }
            if (task.PostprocessSteps != null && task.PostprocessSteps.Count > 0)
            {
                foreach (var step in task.PostprocessSteps)
                {
                    var newStep = new SingleStep();
                    newStep.UpdateInplace(step);
                    PostprocessSteps?.Add(newStep);
                }
            }
        }
    }
}
