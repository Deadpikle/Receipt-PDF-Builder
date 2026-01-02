using System;
using System.IO;
using Avalonia.Platform.Storage;
using HarfBuzzSharp;
using ImageMagick;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Snippets.Font;
using ReceiptPDFBuilder.Interfaces;

namespace ReceiptPDFBuilder.ViewModels;

class MainViewModel : BaseViewModel, IFontResolver
{

    public MainViewModel(IChangeViewModel viewModelChanger) : base(viewModelChanger)
    {
    }

    public async void ChooseFolder()
    {
        var topLevel = TopLevelGrabber?.GetTopLevel();
        if (topLevel is not null)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Pick a folder of files...",
                AllowMultiple = false,
            });
            if (folders.Count == 1)
            {
                var folder = folders[0];
                Console.WriteLine("Chosen folder: " + folder.Path.LocalPath);
                if (Directory.Exists(folder.Path.LocalPath))
                {
                    CreatePDF(folder.Path.LocalPath);
                }
            }
        }
    }

    public byte[]? GetFont(string faceName)
    {
        Console.WriteLine("Getting font {0}", faceName);
        if (faceName == "Noto Sans JP")
        {
            return File.ReadAllBytes("Assets/Fonts/Noto_Sans_JP/static/NotoSansJP-Regular.ttf");
        }
        if (faceName == "Noto Sans JP Bold")
        {
            return File.ReadAllBytes("Assets/Fonts/Noto_Sans_JP/static/NotoSansJP-SemiBold.ttf");
        }
        return null;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        Console.WriteLine("Resolving familyname {0}", familyName);
        if (familyName == "Noto Sans JP")
        {
            if (bold)
            {
                return new FontResolverInfo(familyName + " Bold");
            }
            return new FontResolverInfo(familyName);
        }
        return null;
    }

    // https://forum.pdfsharp.net/viewtopic.php?f=2&t=1025
    private void CreatePDF(string folderName)
    {
        var pdfDoc = new Document();
        var outputFileName = "MyReceipts.pdf";
        var section = pdfDoc.AddSection();
        section.PageSetup.PageFormat = PageFormat.Letter;
        section.PageSetup.PageWidth = "8.5in";
        section.PageSetup.PageHeight = "11in";
        section.PageSetup.TopMargin = "0.5in";
        section.PageSetup.RightMargin = "0.5in";
        section.PageSetup.BottomMargin = "0.5in";
        section.PageSetup.LeftMargin = "0.5in";
        // setup footer for page number
        var footerPar = new Paragraph();
        footerPar.Format.Alignment = ParagraphAlignment.Center;
        footerPar.AddText("--Page ");
        footerPar.AddPageField();
        footerPar.AddText(" of ");
        footerPar.AddNumPagesField();
        footerPar.AddText("--");
        section.Footers.Primary.Add(footerPar);
        //
        var files = Directory.GetFiles(folderName);
        files.Sort();
        GlobalFontSettings.FontResolver = this;
        GlobalFontSettings.FallbackFontResolver = new FailsafeFontResolver();
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var fileName = Path.GetFileName(file);
            if (fileName == ".DS_Store" || fileName == outputFileName)
            {
                continue;
            }
            // TODO: calculate needed width for images based on page width and margins and all that?
            var imageTitlePar = section.AddParagraph();
            imageTitlePar.Format.Alignment = ParagraphAlignment.Center;
            imageTitlePar.Format.Font.Size = 12;
            imageTitlePar.Format.Font.Bold = true;
            imageTitlePar.Format.Font.Name = "Noto Sans JP"; // has english letters in it, too
            imageTitlePar.AddText(fileName);
            section.AddParagraph(); // add empty line for spacing
            // now add the image
            var isPDF = fileName.EndsWith(".pdf");
            var isHEIC = fileName.EndsWith(".HEIC") || fileName.EndsWith(".heic");
            if (isHEIC)
            {
                var convertedDir = Path.Combine(folderName, "converted");
                if (!Directory.Exists(convertedDir))
                {
                    Directory.CreateDirectory(convertedDir);
                }
                var info = new FileInfo(file);
                using var mImage = new MagickImage(info.FullName);
                // Save frame as jpg
                var outputPath = Path.Combine(convertedDir, info.Name + ".jpg");
                mImage.Quality = 80;
                mImage.Scale((uint)Math.Floor(mImage.Width * 0.5), (uint)Math.Floor(mImage.Height * 0.5));
                mImage.Write(outputPath);
                fileName = Path.Combine("Converted", info.Name + ".jpg");
                Console.WriteLine("HEIC image fileName is now {0}", fileName);
            }
            var paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            var image = paragraph.AddImage(fileName);
            image.LockAspectRatio = true;
            image.Width = 400; // can't be too wide now...not sure why...maybe due to margins...
            Console.WriteLine("Added image: {0}", fileName);
            if (isPDF)
            {
                // add other PDF pages
                // see: https://stackoverflow.com/a/65091204/3938401
                var pdfFileToAdd = PdfReader.Open(file);
                imageTitlePar.AddText(string.Format(" (PDF with {0} page{1}) ", 
                    pdfFileToAdd.PageCount, 
                    pdfFileToAdd.PageCount == 1 ? "" : "s"));
                for (var j = 2; j <= pdfFileToAdd.PageCount; j++)
                {
                    section.AddPageBreak();
                    paragraph = section.AddParagraph();
                    paragraph.Format.Alignment = ParagraphAlignment.Center;
                    image = paragraph.AddImage(file + "#" + j);
                    image.LockAspectRatio = true;
                    image.Width = 400;
                }
            }
            if (i < files.Length - 1)
            {
                section.AddPageBreak();
            }
        }
        var pdfRenderer = new PdfDocumentRenderer();
        pdfRenderer.Document = pdfDoc;
        pdfRenderer.WorkingDirectory = folderName;
        pdfRenderer.RenderDocument();
        string filename = Path.Join(folderName, outputFileName);
        pdfRenderer.PdfDocument.Save(filename);
        Console.WriteLine(filename);
    }
}