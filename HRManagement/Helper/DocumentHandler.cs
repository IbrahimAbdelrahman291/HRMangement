namespace HRManagement.Helper
{
    public static class DocumentHandler
    {
        public static string UploadFile(IFormFile file, string FolderName)
        {
            string FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\Files", FolderName);
            string FileName = $"{Guid.NewGuid()}{file.FileName}";
            string FilePath = Path.Combine(FolderPath, FileName);
            using var FileStream = new FileStream(FilePath, FileMode.Create);
            file.CopyTo(FileStream);
            return FilePath;
        }
        public static bool DeleteFile(string FileName)
        {
            bool x = false;
            string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\Files", FileName);
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                x = true;
                return x;
            }
            else
            {
                return x;
            }
        }
    }
}
