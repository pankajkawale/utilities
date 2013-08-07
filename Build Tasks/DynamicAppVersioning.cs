using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Xml;
using System.IO;

namespace DynamicVersioning
{
    public class DynamicAppVersioning : Task
    {

        private string appSettingsKey;

        [Required]
        public string AppSettingsKey
        {
            get { return appSettingsKey; }
            set { appSettingsKey = value; }
        }

        private string configFiles;

        [Required]
        public string ConfigFiles
        {
            get { return configFiles; }
            set { configFiles = value; }
        }

        public override bool Execute()
        {
            try
            {
                List<string> files = configFiles.Split(',').ToList();
                if (files.Count == 0)
                    return false;

                if (string.IsNullOrEmpty(appSettingsKey))
                    return false;

                string xPath = "/configuration/appSettings/add[@key='" + appSettingsKey + "']";
                string uniqueValue = Guid.NewGuid().ToString();
                foreach (var file in files)
                {
                    if (File.Exists(file) == false)
                        continue;

                    var doc = new XmlDocument();
                    doc.Load(file);
                    XmlNode node = doc.SelectSingleNode(xPath);
                    if (node != null)
                    {
                        XmlAttribute attr = node.Attributes["value"];
                        if (attr != null)
                        {
                            attr.Value = uniqueValue;
                        }
                    }

                    doc.Save(file);
                }
                base.Log.LogError("Key successfully updated");
                //BuildEngine3.LogMessageEvent(new BuildMessageEventArgs("Key successfully updated", "", "UpdateSuccessful",
                //                                                      MessageImportance.Normal));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
