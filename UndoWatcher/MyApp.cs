using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Autodesk.AutoCAD.Runtime; 
using Autodesk.AutoCAD.DatabaseServices; 
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput; 
using acApp = Autodesk.AutoCAD.ApplicationServices.Application;

using Autodesk.AutoCAD.Interop;  

namespace UndoWatcher
{
    public class MyApp : IExtensionApplication 
    {
        const long kMaxUndoFileSizeInBytes = 2147483648/*2GB*/;

        static Dictionary<Document, FileInfo> undoFiles = new Dictionary<Document,FileInfo>();
        static MyForm form = new MyForm();

        [CommandMethod("UndoWatcher_ResetUndo")] 
        static public void UndoWatcher_ResetUndo()
        {
            Document doc = acApp.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                removeUndoFile(doc); 
                storeUndoFile(doc);
                showUndoSize(doc);
            }

            //acApp.SetSystemVariable("CMDECHO", 1); 
        }

        public void Initialize()
        {
            form.Show();

            Document doc = acApp.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                storeUndoFile(doc);
                showUndoSize(doc);
                doc.Editor.EnteringQuiescentState += new EventHandler(Editor_EnteringQuiescentState);
            }

            acApp.DocumentManager.DocumentActivated += new DocumentCollectionEventHandler(DocumentManager_DocumentActivated);
            acApp.DocumentManager.DocumentCreated += new DocumentCollectionEventHandler(DocumentManager_DocumentCreated);
            acApp.DocumentManager.DocumentToBeDestroyed += new DocumentCollectionEventHandler(DocumentManager_DocumentToBeDestroyed); 
        }

        static public void storeUndoFile(Document doc)
        {
            AcadApplication acadApp = (AcadApplication)acApp.AcadApplication;

            DirectoryInfo tmpPath = new DirectoryInfo(acadApp.Preferences.Files.TempFilePath);
            FileInfo[] files = tmpPath.GetFiles("UND*.ac$");

            var filePaths =
                from FileInfo file in files
                where file.CreationTimeUtc == files.Max(f => f.CreationTimeUtc)
                select file;

            undoFiles.Add(doc, filePaths.First<FileInfo>());   
        }

        static public void removeUndoFile(Document doc)
        {
            undoFiles.Remove(doc);  
        }

        static public string getFormattedSize(long length)
        {
            string[] sizes = new string[] { "Bytes", "KB", "MB" };

            if (length > 0)
            {
                int power = (int)Math.Min(sizes.GetUpperBound(0), Math.Log(length, 1024));

                length = (long)(length / Math.Pow(1024, power));

                return string.Format("{0} {1}", length, sizes[power]);
            }

            return string.Format("{0} {1}", length, sizes[0]);
        }

        static public void showUndoSize(Document doc)
        {
            FileInfo file = undoFiles[doc];
            file.Refresh();

            if (!file.Exists)
            {
                form.lblSize.Text = "No Undo File";
                return;
            }

            form.lblSize.Text = "File size: " + getFormattedSize(file.Length);   

            double percent = Math.Min(1.0, (double)file.Length / kMaxUndoFileSizeInBytes) * 100;

            if (percent < 50)
                form.pbrUndoSize.BackColor = System.Drawing.Color.LightGreen;
            else if (percent < 75)
                form.pbrUndoSize.BackColor = System.Drawing.Color.Yellow;
            else
                form.pbrUndoSize.BackColor = System.Drawing.Color.Red;

            form.pbrUndoSize.Value = (int)percent;   
        }

        static void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            storeUndoFile(e.Document); 
            e.Document.Editor.EnteringQuiescentState += new EventHandler(Editor_EnteringQuiescentState);
        }

        static void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            showUndoSize(e.Document);
        }

        static void DocumentManager_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            removeUndoFile(e.Document);
        }

        static void Editor_EnteringQuiescentState(object sender, EventArgs e)
        {
            showUndoSize(((Editor)sender).Document);
        }

        public void Terminate()
        {
        }
    }
}
