using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;

namespace Tavisca.Gossamer.UI.BuildTasks
{
    public class AppVersioningTask : Task
    {

        private string _keyToReplace;

        [Required]
        public string KeyToReplace
        {
            get { return _keyToReplace; }
            set { _keyToReplace = value; }
        }

        private string _files;

        [Required]
        public string Files
        {
            get { return _files; }
            set { _files = value; }
        }

        public override bool Execute()
        {
            try
            {
                List<string> files = Files.Split(',').ToList();
                if (files.Count == 0)
                    return false;

                if (string.IsNullOrEmpty(KeyToReplace))
                    return false;
                
                var uniqueRevisionNumber = Guid.NewGuid().ToString();

                foreach (var file in files)
                {
                    if (File.Exists(file) == false)
                        continue;

                    var fileText = File.ReadAllText(file);
                    fileText = fileText.Replace(KeyToReplace, uniqueRevisionNumber);
                    File.WriteAllText(file, fileText);
                }
                base.Log.LogMessage("Application version number successfully updated");
                
                return true;
            }
            catch (Exception ex)
            {
                base.Log.LogError("Application Versioning task failed with error : " + ex.Message);
                return false;
            }
        }
    }
}
