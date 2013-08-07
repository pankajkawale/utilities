using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.IO.Compression;

namespace BuildTasks
{
    public class ResourceCombiner : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var fileParam = context.Request["files"];
            var enableCompression = CanCompress(context.Request.Headers["Accept-Encoding"]);
            var filesWritten = WriteFilesToOutputStream(context, fileParam, enableCompression);

            if (filesWritten)
            {
                AddContentType(context, context.Request["type"]);
                AddHeaders(context, enableCompression);
            }
            else
            {
                context.Response.StatusCode = 404;
            }

            context.Response.Flush();
        }

        private bool CanCompress(string acceptEncoding)
        {
            return (string.IsNullOrWhiteSpace(acceptEncoding) == false && acceptEncoding.IndexOf("gzip") != -1);
        }

        private void AddHeaders(HttpContext context, bool enableCompression)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.Private);
            context.Response.Cache.SetExpires(DateTime.Now.AddYears(1));
            if (enableCompression)
            {
                context.Response.AppendHeader("Content-Encoding", "gzip");
            }
        }

        private void AddContentType(HttpContext context, string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileType))
                fileType = "css";

            if (string.Compare(fileType, "css", true) == 0)
                context.Response.ContentType = "text/css";
            else if (string.Compare(fileType, "js", true) == 0)
                context.Response.ContentType = "application/x-javascript";
        }

        private bool WriteFilesToOutputStream(HttpContext context, string fileParam, bool enableCompression)
        {
            if (string.IsNullOrWhiteSpace(fileParam))
                return false;

            var files = fileParam.Split(',');

            if (files.Count() == 0)
                return false;

            // If bundle is requested - 
            // Check if file for the bundle exists
            // If exists, read from it and apply compression
            // If it does not exist, read files from bundle, write them to a single file and apply compression

            var content = new byte[] { };
            if (files.Count() == 1)
            {
                content = ReadContent(files[0], context);
            }
            else
            {
                for (int i = 0; i < files.Length; i++)
                {
                    content = content.Concat(ReadContent(files[i], context)).ToArray();
                }
            }

            using (var memorystream = new MemoryStream())
            {
                using (var writer = enableCompression ? (Stream)(new GZipStream(memorystream, CompressionMode.Compress)) : memorystream)
                {
                    writer.Write(content, 0, content.Length);
                    writer.Close();
                }

                var responseBytes = memorystream.ToArray();

                context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            }

            return true;
        }

        private IEnumerable<byte> ReadBundle(string name, HttpContext context)
        {
            //If file with combined content for a bundle exists, read it
            IEnumerable<byte> content = ReadFile(name + ".txt", context);

            // else combine the files and write content to file
            if (content.Count() == 0)
            {
                var bundle = _bundles.Find(b => string.Compare(b.Name, name, true) == 0);
                bundle.Files.ForEach(file =>
                {
                    content = content.Concat(ReadFile(file, context)).Concat(_space);
                });

                // Do not create files for localhost as it could get added to source control
                if (context.Request.Url.AbsoluteUri.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) == -1)
                    WriteToFile(context.Server.MapPath(name + ".txt"), content);
            }

            return content;
        }

        private byte[] ReadFile(string path, HttpContext context)
        {
            var content = new byte[] { };
            path = context.Server.MapPath(path).Trim();
            if (File.Exists(path))
            {
                content = File.ReadAllBytes(path).TrimBOM();
            }

            return content;
        }

        private void WriteToFile(string path, IEnumerable<byte> content)
        {
            File.WriteAllBytes(path, content.ToArray());
        }

        private byte[] ReadContent(string file, HttpContext context)
        {
            if (_bundles.Contains(file))
            {
                return ReadBundle(file, context).ToArray();
            }

            return ReadFile(file, context);
        }

        private byte[] _space = new byte[] { (byte)' ' };

        private List<Bundle> _bundles = new List<Bundle>()
                                            {
                                                new Bundle()
                                                    {
                                                        Name = "BasicPlugins",
                                                        Files = new List<string>()
                                                                    {
                                                                        "config.js",
                                                                        "infrastructure/libraries/jquery.js",
                                                                        "infrastructure/libraries/jquery-ui.js",
                                                                        "infrastructure/libraries/underscore.js",
                                                                        "infrastructure/libraries/backbone.js",
                                                                        "infrastructure/libraries/prettify.js",
                                                                        "infrastructure/libraries/bootstrap.min.js",
                                                                        "infrastructure/libraries/mustache.js",
                                                                        "infrastructure/libraries/jquery.cookies.js",
                                                                        "infrastructure/libraries/jquery.nicescroll.min.js",
                                                                        "infrastructure/libraries/avgrund.js",
                                                                        "infrastructure/confirmationDialog.js"
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "GossamerUIPlugins",
                                                        Files = new List<string>()
                                                                    {
                                                                        "infrastructure/utils.js",
                                                                        "infrastructure/cookies.js",
                                                                        "infrastructure/String.js",
                                                                        "eventManager.js",
                                                                        "infrastructure/arrays.js",
                                                                        "infrastructure/ajax.js", 
                                                                        "infrastructure/urlfactory.js",
                                                                        "infrastructure/templatemanager.js",
                                                                        "models/user.js",
                                                                        "models/apps.js",
                                                                        "services/authenticationService.js",
                                                                        "infrastructure/storage.js",
                                                                        "models/identityCollection.js",
                                                                        "controllers/loginController.js",
                                                                        "controllers/masterPageController.js",
                                                                        "controllers/loaderController.js",
                                                                        "views/masterPage/masterPageView.js",
                                                                        "views/header/headerView.js",
                                                                        "views/loader/loaderView.js",
                                                                        "views/sessionExpired/sessionExpiredView.js",
                                                                        "router.js",
                                                                        "infrastructure/prettyinput.js"
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "GraphUI",
                                                        Files = new List<string>()
                                                                    {
                                                                        "infrastructure/libraries/graphUI/gpu.js",
                                                                        "infrastructure/libraries/graphUI/main.js",
                                                                        "infrastructure/libraries/graphUI/jquery.mousewheel.js",
                                                                        "infrastructure/libraries/graphUI/raphael.js",
                                                                        "infrastructure/libraries/graphUI/springy.js",
                                                                        "infrastructure/libraries/dyngraphs.js",
                                                                        "views/dataExplorer/dataExplorerView.js",
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "DBControllers",
                                                        Files = new List<string>()
                                                                    {
                                                                        "controllers/applicationController.js",
                                                                        "controllers/leftNavBarController.js",
                                                                        "controllers/toggleEnvironmentController.js",
                                                                        "controllers/deploymentController.js",
                                                                        "controllers/CatalogController.js",
                                                                        "controllers/schemaController.js",
                                                                        "controllers/relationController.js",
                                                                        "controllers/cannedListController.js",
                                                                        "controllers/designController.js",
                                                                        "controllers/tagsController.js",
                                                                        "controllers/comingSoonController.js",
                                                                        "controllers/articleController.js",
                                                                        "controllers/connectionController.js",
                                                                        "controllers/testHarnessController.js",
                                                                        "controllers/dataExplorerController.js",
                                                                        "controllers/userController.js",
                                                                        "controllers/feedbackController.js"
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "DBCollection",
                                                        Files = new List<string>()
                                                                    {
                                                                        "services/invoiceService.js",
                                                                    "models/applicationCollection.js",
                                                                    "models/schemaCollection.js",
                                                                    "models/relationCollection.js",
                                                                    "models/cannedListCollection.js",
                                                                    "models/catalogCollection.js",
                                                                    "models/deploymentCollection.js",
                                                                    "models/articleCollection.js",
                                                                    "models/connectionCollection.js",
                                                                    "models/emailTemplateCollection.js",
                                                                    "models/baseObject.js",
                                                                    "models/serviceObject.js",
                                                                    "infrastructure/libraries/jquery.ui.toggleswitch.js",
                                                                    "infrastructure/libraries/jquery.toggle.buttons.js",
                                                                    "models/propertyTemplateFactory.js",
                                                                    "models/user.js",
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "DBBasicViews",
                                                        Files = new List<string>()
                                                                    {
                                                                    "views/leftNavBar/leftNavBarView.js",
                                                                    "views/toggleEnvironment/toggleEnvironmentView.js",
                                                                    "views/comingSoon/comingSoonView.js",
                                                                    "router.js",
                                                                    "views/dashboard/dashboardMasterPage.js",
                                                                    "views/designPage/graph/graphView.js",
                                                                    "views/dashboard/graph.js",
                                                                    "views/dashboard/graphWidget/graphWidget.js",
                                                                    "views/dashboard/addWidget/addWidget.js",
                                                                    "views/configure/general/generalView.js",
                                                                    "views/configure/push/pushView.js",
                                                                    "views/configure/settings/settingsConfigureView.js",
                                                                    "views/configure/advance/advanceConfigureView.js",
                                                                    "views/configure/delete/appDeleteView.js",
                                                                    "views/dataExplorer/dataExplorerView.js",
                                                                    "views/configure/social/socialView.js",
                                                                    "views/configure/delete/appDeleteView.js",
                                                                    "views/email/general/emailGeneralView.js",
                                                                    "views/email/template/emailTemplateView.js",
                                                                    "views/email/triggers/emailTriggersView.js"
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "DBOtherViews",
                                                        Files = new List<string>()
                                                                    {
                                                                    "infrastructure/validations.js",
                                                                    "views/designPage/threeList/threeListView.js",
                                                                    "infrastructure/editInPlace.js",
                                                                    "infrastructure/libraries/tagit.js",
                                                                    "views/designPage/schemaDetailsView/schemaDetailsView.js",
                                                                    "views/designPage/propertyView/propertyView.js",
                                                                    "views/designPage/cannedListDetailsView/cannedListDetailsView.js",
                                                                    "views/designPage/relationDetailsView/relationDetailsView.js",
                                                                    "views/designPage/cannedListDetailsView/listItemView.js",
                                                                    "views/designPage/deploymentPublishResult/deploymentPublishResultView.js",
                                                                    "views/designPage/cancelMerge/cancelMergeView.js",
                                                                    "views/designPage/propertyDetailsView/propertyDetailsView.js",
                                                                    "views/designPage/singleError/singleErrorView.js",
                                                                    "views/designPage/articleGrid/articleGridView.js",
                                                                    "views/designPage/apikeysView/apikeysView.js",
                                                                    "views/designPage/articleProperties/geographyView/geographyView.js",
                                                                    "views/loadingScreen/loadingScreenView.js",
                                                                    "views/designPage/articleCreateView/articleView.js",
                                                                    "views/designPage/articleProperties/cannedListLookupView/cannedListLookupView.js",
                                                                    "views/designPage/articleProperties/dateControlView/dateControlView.js",
                                                                    "views/designPage/articleProperties/timeControlView/timeControlView.js",
                                                                    "views/designPage/articleProperties/fileControlView/fileControlView.js",
                                                                    "views/designPage/articleProperties/dateTimeControlView/dateTimeControlView.js",
                                                                    "views/designPage/articleProperties/dateTimeWrapperView/dateTimeWrapperView.js",
                                                                    "views/designPage/articleProperties/booleanControlView/booleanControlView.js",
                                                                    "views/designPage/articleProperties/textboxControlView/textboxControlView.js",
                                                                    "views/designPage/articleProperties/textareaControlView/textareaControlView.js",
                                                                    "views/designPage/articleProperties/geographyView.js/geographyView.js",
                                                                    "views/designPage/rawView/rawView.js",
                                                                    "views/designPage/testHarness/testharness.settings.js",
                                                                    "views/designPage/testHarness/testHarnessView.js"
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "DBPlugins",
                                                        Files = new List<string>()
                                                                    {
                                                                    "infrastructure/uiPlugins.js",
                                                                    "infrastructure/libraries/jquery.validate.min.js",
                                                                    "infrastructure/libraries/select2.min.js",
                                                                    "infrastructure/libraries/jquery-ui-timepicker-addon.js",
                                                                    "infrastructure/libraries/biginteger.js",
                                                                    "infrastructure/libraries/additional-methods.js",
                                                                    "infrastructure/libraries/jquery.linkbutton.js",
                                                                    "infrastructure/libraries/jquery.pagination.js",
                                                                    "infrastructure/libraries/jquery.flip.min.js",
                                                                    "infrastructure/libraries/gmap3.min.js",
                                                                    "infrastructure/libraries/jquery.dateLists.js",
                                                                    "infrastructure/libraries/codemirror/codemirror.js",
                                                                    "infrastructure/libraries/codemirror/codemirror.javascript.js",
                                                                    "infrastructure/libraries/codemirror/jquery.codemirror.js",
                                                                    "infrastructure/libraries/codemirror/closetag.js",
                                                                    "infrastructure/libraries/codemirror/formatting.js",
                                                                    "infrastructure/libraries/codemirror/css.js",
                                                                    "infrastructure/libraries/codemirror/xml.js",
                                                                    "infrastructure/libraries/codemirror/htmlmixed.js",
                                                                    "infrastructure/libraries/prettify.js",
                                                                    "infrastructure/libraries/jsbeautifier.js",
                                                                    "infrastructure/libraries/jquery.tipsy.js",
                                                                    "infrastructure/libraries/highcharts/highcharts.js"
                                                                    }
                                                    },
                                                    new Bundle()
                                                    {
                                                        Name = "UploadPlugins",
                                                        Files = new List<string>()
                                                                    {
                                                                    "infrastructure/libraries/upload/jquery.iframe-transport.js",
                                                                    "infrastructure/libraries/upload/jquery.fileupload.js",
                                                                    "infrastructure/libraries/upload/jquery.fileupload-fp.js",
                                                                    "infrastructure/fileUpload.js"
                                                                    }
                                                    }
                                            };
    }

    public class Bundle
    {
        public string Name { get; set; }

        public List<string> Files { get; set; }
    }

    public static class Extensions
    {
        public static byte[] TrimBOM(this byte[] content)
        {
            // BOM check. If BOM exists, strip it as it adds special characters to response and browser invalidates it
            if (content.Length > 2 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            {
                return content.Skip(3).ToArray();
            }

            return content;
        }

        public static bool Contains(this List<Bundle> bundles, string name)
        {
            return bundles != null && bundles.Any(bundle => string.Compare(bundle.Name, name, true) == 0);
        }
    }

}
