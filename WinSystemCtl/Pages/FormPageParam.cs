using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Pages
{
    public enum FormPageOperationType
    {
        Create,
        Edit
    }

    public class FormPageParam<T> where T: class
    {
        public FormPageOperationType OperationType { get; set; }

        public T? Data { get; set; }

        public Action<T>? CreateCallback { get; set; }

        public Action? EditCallback { get; set; }

        public FormPageParam(Action<T> createCallback)
        {
            OperationType = FormPageOperationType.Create;
            CreateCallback = createCallback;
        }

        public FormPageParam(T data, Action? editCallback)
        {
            OperationType = FormPageOperationType.Edit;
            Data = data;
            EditCallback = editCallback;
        }
    }
}
