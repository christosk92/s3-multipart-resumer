using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace S3ResumeTest.Handlers
{
    public interface ffapi
    {
        [Post("/oauth/token")]
        Task<Authentication> Auth([Body]Credentials credentials);

        [Put("/api/upload/{id}")]
        [Headers("Authorization: Bearer")]
        Task<Upload> UpdateUpload([Body]Upload upload, string id);

        [Get("/api/upload/file/{fname}")]
        [Headers("Authorization: Bearer")]
        Task<Upload> GetUploadOnFileName(string fname);

        [Post("/api/upload")]
        [Headers("Authorization: Bearer")]
        Task<Upload> CreateUpload([Body]Upload upload);
    }
}