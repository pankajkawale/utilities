﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Web.SessionState;
using System.IO;

namespace Tavisca.Gossamer.UI.BuildTasks
{
    public class FileDownloadHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                var url = GetRequestUrl(context);
                var request = WebRequest.Create(url);
                SetSessionHeaders(context, request);
                var response = request.GetResponse();
                var responseStream = response.GetResponseStream();

                var filename = response.Headers["X-FileName"];
                context.Response.ContentType = response.ContentType;
                if (string.IsNullOrWhiteSpace(context.Request.QueryString["attach"]))
                {
                    context.Response.AddHeader("Content-Disposition", "inline; filename=" + filename);
                }
                else
                {
                    context.Response.AddHeader("Content-Disposition", "attachment; filename=" + filename);
                }
                CopyStream(responseStream, context.Response.OutputStream);
            }
            catch (AccessViolationException)
            {
                context.Response.Write("You are not authorized to access the requested data.");
            }
            catch (Exception)
            {
                context.Response.Write("An error occurred while processing the request.");
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
                output.Flush();
            }
        }

        public string GetRequestUrl(HttpContext context)
        {
            var url = context.Request.QueryString["fileurl"];
            return url;
        }

        private void SetSessionHeaders(HttpContext context, WebRequest request)
        {
            if (context.Request.Cookies.Count != 0)
            {
                var httpCookie = context.Request.Cookies["__app_session"];
                if (httpCookie != null)
                {
                    request.Headers.Add("api-sessiontoken", HttpUtility.UrlDecode(httpCookie.Value));
                }
            }
        }

        private string UploadToFileSystem(string filename, Stream file)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory + "images\\";
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            int bytesRead = 0;
            var buffer = new byte[1024];
            string outFile = basePath.EndsWith(@"\") ? string.Format(@"{0}{1}", basePath, filename) : string.Format(@"{0}\{1}", basePath, filename);
            using (var output = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                do
                {
                    bytesRead = file.Read(buffer, 0, 1024);
                    if (bytesRead > 0)
                        output.Write(buffer, 0, bytesRead);
                } while (bytesRead != 0);
                output.Close();
            }

            return outFile;
        }
    }
}
