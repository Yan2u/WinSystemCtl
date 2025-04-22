using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email.DataProvider;
using WinSystemCtl.Core;
using WinSystemCtl.Core.Data;

namespace WinSystemCtl.Pages
{
    public class SingleStepPageViewModel : RelatedViewModel<SingleStepPage>
    {
        private SingleStep _step;
        public SingleStep Step
        {
            get => _step;
            set => Set(ref _step, value);
        }

        private SingleStep? _target = null;
        private FormPageParam<SingleStep> _param;

        public string GenTitle(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return App.GetString("lang_EditSingleStep") + $": {Path.GetFileName(name)}";
            }
            else
            {
                return App.GetString("lang_EditSingleStep");
            }
        }

        public void Close(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ViewModel.NavigateBack();
        }

        public void Confirm(object sender, RoutedEventArgs e)
        {
            switch (_param.OperationType)
            {
                case FormPageOperationType.Create:
                    SingleStep newStep = new();
                    newStep.UpdateInplace(Step);
                    _param.CreateCallback?.Invoke(newStep);
                    break;
                case FormPageOperationType.Edit:
                    _target?.UpdateInplace(Step);
                    _param.EditCallback?.Invoke();
                    break;
                default:
                    break;
            }
            MainWindow.Instance.ViewModel.NavigateBack();
        }
        public void EnvironmentItemDelete(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var key = element.Tag as string;
                for (int i = Step.EnvironmentVariables.Count - 1; i >=0 ; --i)
                {
                    if (Step.EnvironmentVariables[i].Key == key)
                    {
                        Step.EnvironmentVariables.RemoveAt(i);
                    }
                }
            }
        }

        public void EnvironmentItemLoad(object sender, RoutedEventArgs e)
        {
            var env = Environment.GetEnvironmentVariables();
            foreach (var key in env.Keys)
            {
                Step.EnvironmentVariables.Add(new((string)key, (string)env[key]));
            }
        }

        public void EnvironmentItemAdd(object sender, RoutedEventArgs e)
        {
            Step.EnvironmentVariables ??= new ObservableCollection<EnvironmentVarPair>();
            Step.EnvironmentVariables.Add(new(string.Empty, string.Empty));
        }

        public void EnvironmentItemClear(object sender, RoutedEventArgs e)
        {
            Utils.MessageBox(App.GetString("lang_Warning"), App.GetString("lang_ConfirmClear"), (result) =>
            {
                if (result == ContentDialogResult.Primary)
                {
                    Step.EnvironmentVariables?.Clear();
                }
            });
        }

        public void PickProgram(object sender, RoutedEventArgs e)
        {
            Step.Program = Utils.PickSingleFile(MainWindow.Instance, "All", "*.*") ?? Step.Program;
        }

        public void PickWorkingDirectory(object sender, RoutedEventArgs e)
        {
            Step.WorkingDirectory = Utils.PickSingleFolder(MainWindow.Instance) ?? Step.WorkingDirectory;
        }

        public void PickInputFile(object sender, RoutedEventArgs e)
        {
            Step.Input = Utils.PickSingleFile(MainWindow.Instance, "All", "*.*") ?? Step.Input;
        }

        public void PickOutputFile(object sender, RoutedEventArgs e)
        {
            Step.Output = Utils.PickSingleFile(MainWindow.Instance, "All", "*.*") ?? Step.Output;
        }

        public void Navigated(NavigationEventArgs e)
        {
            if (e.Parameter is FormPageParam<SingleStep> p)
            {
                _param = p;
                if (p.OperationType == FormPageOperationType.Edit)
                {
                    _target = p.Data;
                    Step.UpdateInplace(p.Data);
                }
                else
                {
                    _target = null;
                    Step.Reset();
                }

                _relatedView.IsEnabled = true;
            }
            else
            {
                _relatedView.IsEnabled = false;
            }
        }

        public SingleStepPageViewModel(SingleStepPage relatedView) : base(relatedView)
        {
            Step = new();
        }
    }
}
