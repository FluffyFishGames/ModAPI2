using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ModAPI.ViewModels;
using System;

namespace ModAPI
{
    public class ViewLocator : IDataTemplate
    {
        public IControl Build(object data)
        {
            var type = data.GetType();
            var name = (type + "View").Replace("ViewModel", "View");
            var viewType = Type.GetType(name);

            if (viewType != null)
            {
                return (Control)Activator.CreateInstance(viewType)!;
            }
            else
            {
                if (type.BaseType != null)
                    return Build(data, type.BaseType);
                else 
                    return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public IControl Build(object data, Type type)
        {
            if (type == null)
                type = data.GetType();
            var name = (type.FullName + "View").Replace("ViewModel", "View");
            var viewType = Type.GetType(name);

            if (viewType != null)
            {
                return (Control)Activator.CreateInstance(viewType)!;
            }
            else
            {
                if (type.BaseType != null)
                    return Build(data, type.BaseType);
                else
                    return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
