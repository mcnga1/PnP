using Fclp;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Principal;

namespace Provisioning.Framework
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if true
            Console.WriteLine(string.Format("{0}", Version));
            string targetSiteUrl = string.Empty;

            // http://fclp.github.io/fluent-command-line-parser/
            var p = new FluentCommandLineParser();
            p.Setup<string>('u', "url")
                .Callback(v => targetSiteUrl = v)
                .Required();

            var parseResult = p.Parse(args);

            if (parseResult.HelpCalled) return;

            if (parseResult.HasErrors)
            {
                Console.WriteLine(string.Format("{0}", parseResult.ErrorText));
                return;
            }

            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(@".", "");
            string templateName = @"provision-schema.xml";

            // Template
            ProvisioningTemplate template = provider.GetTemplate(templateName);

            // Get the available, valid templates
            var templates = provider.GetTemplates();
            foreach (var template1 in templates)
            {
                Console.WriteLine("Found template with ID {0}", template1.Id);
            }

            // Get access to target site and apply template
            using (var ctx = new ClientContext(targetSiteUrl))
            {
                //Provide count and pwd for connecting to the source
                ctx.Credentials = GetCredentials(targetSiteUrl);

                ProvisioningTemplateApplyingInformation pta = new ProvisioningTemplateApplyingInformation();
                pta.ProgressDelegate = (message, step, total) =>
                {
                    Console.WriteLine(string.Format("Applying template - Step {0}/{1} : {2} ", step, total, message));
                };

                // Apply template to existing site
                ctx.Web.ApplyProvisioningTemplate(template, pta);
            }
#else
            bool interactiveLogin = true;
            string templateSiteUrl = "https://bertonline.sharepoint.com/sites/provdemoget";
            string targetSiteUrl = "https://bertonline.sharepoint.com/sites/provdemoapply";
            // Office 365: username@tenant.onmicrosoft.com
            // OnPrem: DOMAIN\Username
            string loginId = "bert.jansen@bertonline.onmicrosoft.com";

            // Get pwd from environment variable, so that we do to need to show that.
            string pwd = "";
            if (interactiveLogin)
            {
                pwd = GetInput("Password", true);
            }
            else
            {
                pwd = Environment.GetEnvironmentVariable("MSOPWD", EnvironmentVariableTarget.User);
            }

            if (string.IsNullOrEmpty(pwd))
            {
                Console.WriteLine("MSOPWD user environment variable empty or no password was specified, cannot continue. Press any key to abort.");
                Console.ReadKey();
                return;
            }

            // Template
            ProvisioningTemplate template;

            // Get access to source site
            using (var ctx = new ClientContext(templateSiteUrl))
            {
                //Provide count and pwd for connecting to the source
                ctx.Credentials = GetCredentials(targetSiteUrl, loginId, pwd);

                ProvisioningTemplateCreationInformation ptc = new ProvisioningTemplateCreationInformation(ctx.Web);
                ptc.ProgressDelegate = (message, step, total) =>
                {
                    Console.WriteLine(string.Format("Getting template - Step {0}/{1} : {2} ", step, total, message));
                };

                // Get template from existing site
                template = ctx.Web.GetProvisioningTemplate(ptc);
            }

            // Save template using XML provider
            XMLFileSystemTemplateProvider provider = new XMLFileSystemTemplateProvider(@"c:\temp\pnpprovisioningdemo", "");
            string templateName = "template.xml";
            provider.SaveAs(template, templateName);

            // Load the saved model again
            ProvisioningTemplate p2 = provider.GetTemplate(templateName);

            // Get the available, valid templates
            var templates = provider.GetTemplates();
            foreach (var template1 in templates)
            {
                Console.WriteLine("Found template with ID {0}", template1.Id);
            }

            // Get access to target site and apply template
            using (var ctx = new ClientContext(targetSiteUrl))
            {
                //Provide count and pwd for connecting to the source
                ctx.Credentials = GetCredentials(targetSiteUrl, loginId, pwd);

                ProvisioningTemplateApplyingInformation pta = new ProvisioningTemplateApplyingInformation();
                pta.ProgressDelegate = (message, step, total) =>
                {
                    Console.WriteLine(string.Format("Applying template - Step {0}/{1} : {2} ", step, total, message));
                };

                // Apply template to existing site
                ctx.Web.ApplyProvisioningTemplate(template, pta);
<<<<<<< HEAD
            }
#endif
        }

        private static string Version
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
                return String.Format("{4} v{0}.{1}.{2}.{3}", fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart, fvi.ProductPrivatePart, fvi.ProductName);
            }
        }

        private static string GetInput(string label, bool isPassword)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0} : ", label);
            Console.ForegroundColor = ConsoleColor.Gray;

            string strPwd = "";

            for (ConsoleKeyInfo keyInfo = Console.ReadKey(true); keyInfo.Key != ConsoleKey.Enter; keyInfo = Console.ReadKey(true))
            {
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (strPwd.Length > 0)
                    {
                        strPwd = strPwd.Remove(strPwd.Length - 1);
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                }
                else if (keyInfo.Key != ConsoleKey.Enter)
                {
                    if (isPassword)
                    {
                        Console.Write("*");
                    }
                    else
                    {
                        Console.Write(keyInfo.KeyChar);
                    }
                    strPwd += keyInfo.KeyChar;
                }
            }
            Console.WriteLine("");

            return strPwd;
        }

        private static ICredentials GetCredentials(string siteUrl)
        {
            return GetCredentials(siteUrl, string.Empty, string.Empty, string.Empty);
        }

        private static ICredentials GetCredentials(string siteUrl, string loginId, string pwd, string domain)
        {
            var securePassword = new SecureString();
            foreach (char c in pwd.ToCharArray()) securePassword.AppendChar(c);

            if (siteUrl.ToLower().Contains("sharepoint.com"))
            {
                return new SharePointOnlineCredentials(loginId, securePassword);
            }

            string userInfo = string.Empty;
            NetworkCredential credential = null;
            bool useWindowsAuthentication = string.IsNullOrEmpty(loginId);

            if (useWindowsAuthentication)
            {
                credential = CredentialCache.DefaultNetworkCredentials;
                userInfo = WindowsIdentity.GetCurrent().Name;
            }
            else
            {
                credential = new NetworkCredential(loginId, securePassword, domain);
                userInfo = string.Format(@"{0}\{1}", credential.Domain, credential.UserName);
            }

            Log.Debug("GetCredentials", string.Format("{0} [Settings.Default.UseWindowsAuthentication={1}]", userInfo, useWindowsAuthentication));
            return credential;
        }
    }
}