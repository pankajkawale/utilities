using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildTasks
{
    public class TemplateCombiningTask : Task
    {
        private string _outputFileName;
        [Required]
        public string OutputFileName
        {
            get { return _outputFileName; }
            set { _outputFileName = value; }
        }

        private string _basePath;
        [Required]
        public string BasePath
        {
            get { return _basePath; }
            set { _basePath = value; }
        }

        private  StringBuilder _fullOutput = new StringBuilder();
        private int _counter = 0;

        public override bool Execute()
        {
            try
            {
                File.Delete(BasePath + @"\" + OutputFileName);
                Directory.GetFiles(BasePath, "*.*", SearchOption.AllDirectories).ToList().ForEach(x =>
                                                                                                      {
                                                                                                          if (
                                                                                                              x.EndsWith
                                                                                                                  (".html") ||
                                                                                                              x.EndsWith
                                                                                                                  (".htm"))
                                                                                                              CombineFile
                                                                                                                  (x);
                                                                                                          _counter++;
                                                                                                      });
                _fullOutput = new StringBuilder("<div class='hidden'>\n").Append(_fullOutput).Append("</div>");

                var inBytes = Encoding.Default.GetBytes(_fullOutput.ToString());
                File.Create(BasePath + @"\" + OutputFileName).Write(inBytes, 0, inBytes.Length);

                Log.LogMessage(_counter + " files combined into " + BasePath + @"\" + OutputFileName + ".");

                return true;
            }
            catch (Exception e)
            {
                base.Log.LogErrorFromException(e);

                return false;
            }
        }

        private void CombineFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var contentStringB = new StringBuilder();
            lines.ToList().ForEach(l => contentStringB.AppendLine(l));
            var templateName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            _fullOutput.AppendFormat("<div id='{0}'>{1}</div>\n", templateName, contentStringB);
        }
    }
}
