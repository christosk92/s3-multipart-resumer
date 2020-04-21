using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace S3ResumeTest.Handlers
{
    public class Upload : FilmFetchBaseDto
    {
        public UploadType Status { get; set; }
        public string S3Id { get; set; }
        public string filename { get; set; }
        public string Key { get; set; }
    }
}
