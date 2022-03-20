using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AbpCodeGeneration
{
    internal class CodeGenerationDialogWindow:DialogWindow
    {
        public CodeGenerationDialogWindow(string title, Control content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content;
            Title = title;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}
