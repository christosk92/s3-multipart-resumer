using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Refit;
using S3ResumeTest.Handlers;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace S3ResumeTest
{
    class Program
    {
        private static readonly string endpoint = "https://s3.eu-central-1.wasabisys.com";
        private static string bucketName = "call4musicff";
        private static IAmazonS3 s3Client;
        public static Logger Log;
        private static ffapi ffApi; 
        public static Guid SessionId = Guid.NewGuid();

        public static IConfigurationRoot configP = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .AddEnvironmentVariables()
             .Build();
        public static Credentials payloadDev = new Credentials
        {
            audience = configP["audience"],
            clientid = configP["client_id"],
            clientsecret = configP["client_secret"],
            granttype = "password",
            username = configP["ff_username"],
            password = configP["ff_password"]
        };
        private static string Access_token;
        static async Task Main(string[] args)
        {
            ffApi = RestService.For<ffapi>(configP["auth_url"]);
            Authentication Authresponse = new Authentication();
            Log = new LoggerConfiguration()
                  .Enrich.FromLogContext()
                  .WriteTo.Console()
                  .WriteTo.File($"{SessionId}.txt", rollOnFileSizeLimit: true)
                  .CreateLogger();
            try
            {
                Authresponse = await ffApi.Auth(payloadDev);
            }
            catch (ApiException ex)
            {
                if (ex.ReasonPhrase == "Forbidden")
                {
                    Log.Error("Invalid auth");
                }
                else
                {
                    Log.Error(ex.ToString());
                }
                Authresponse = null;
            }
            if (Authresponse != null)
            {
                Access_token = Authresponse.AccessToken;
                Console.WriteLine("Authenticated");
                ffApi = RestService.For<ffapi>("http://localhost:60341", new RefitSettings()
                {
                    AuthorizationHeaderValueGetter = () => Task.FromResult(Access_token)
                });
                var config = new AmazonS3Config { ServiceURL = endpoint };
                var accessKey = configP["s3-access-key"];
                var secretAccesskey = configP["s3-secret-key"];
                s3Client = new AmazonS3Client(accessKey, secretAccesskey, config);
                while (true)
                {
                    Console.WriteLine("Enter file path: ");
                    string filePath = Console.ReadLine().Replace("\"", "");
                    await UploadFileAsync(filePath);
                }
            }
        }
        private static string generateID()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static async Task UploadFileAsync(string filePath)
        {
            try
            {
                List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
                var j = new FileInfo(filePath);
                //do some logic...
                var uploadToApi = await ffApi.GetUploadOnFileName(j.Name);
                string uploadId = uploadToApi.S3Id;
                if (uploadToApi.Id == null || uploadToApi.Status == UploadType.Uploaded)
                {
                    string key = generateID();

                    Amazon.S3.Model.InitiateMultipartUploadRequest request = new InitiateMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = key,
                        CannedACL = S3CannedACL.AuthenticatedRead
                    };
                    request.Metadata.Add("fname", j.Name);
                    request.Metadata.Add("transfer-created", DateTime.UtcNow.ToString());
                    request.Metadata.Add("up-version", "0.1;closed");
                    InitiateMultipartUploadResponse response = await s3Client.InitiateMultipartUploadAsync(request);
                    uploadId = response.UploadId;
                    uploadToApi = await ffApi.CreateUpload(new Upload
                    {
                        filename = j.Name,
                        S3Id = uploadId,
                        Key = key,
                        Status = UploadType.Initialized
                    });
                }
                Console.WriteLine("uploadid: " + uploadId);
                // Upload parts.
                long contentLength = new FileInfo(filePath).Length;
                long partSize = 50000000; // 50 mb
                Console.WriteLine("part size: " + partSize + " bytes");
                Console.WriteLine("Finding parts");
                //first we check for the parts..
                ListPartsRequest listPartsRequest = new ListPartsRequest
                {
                    BucketName = bucketName,
                    Key = uploadToApi.Key,
                    UploadId = uploadId
                };
                var listParts = await s3Client.ListPartsAsync(listPartsRequest);
                int currentPart = Math.Max(listParts.NextPartNumberMarker, 0);
                long filePosition = currentPart * partSize;
                for (int i = currentPart + 1; filePosition < contentLength; i++)
                {
                    Console.WriteLine("Uploading part index: " + i);
                    UploadPartRequest uploadRequest = new UploadPartRequest
                    {
                        BucketName = bucketName,
                        Key = uploadToApi.Key,
                        UploadId = uploadId,
                        PartNumber = i,
                        PartSize = partSize,
                        FilePosition = filePosition,
                        FilePath = filePath
                    };
                    // Track upload progress.
                    uploadRequest.StreamTransferProgress +=
                        new EventHandler<StreamTransferProgressArgs>(UploadPartProgressEventCallback);
                    // Upload a part and add the response to our list.
                    var res = await s3Client.UploadPartAsync(uploadRequest);
                    filePosition += partSize;
                }
                // Setup to complete the upload.
                listParts = await s3Client.ListPartsAsync(listPartsRequest);
                List<PartETag> t = new List<PartETag>();
                foreach(var p in listParts.Parts)
                {
                    t.Add(new PartETag
                    {
                        ETag = p.ETag,
                        PartNumber = p.PartNumber
                    });
                }
                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = uploadToApi.Key,
                    UploadId = uploadId
                };
                completeRequest.AddPartETags(t);

                // Complete the upload.
                CompleteMultipartUploadResponse completeUploadResponse =
                    await s3Client.CompleteMultipartUploadAsync(completeRequest);
                uploadToApi.Status = UploadType.Uploaded;
                var updated = await ffApi.UpdateUpload(uploadToApi, uploadId);
            }
            catch (Exception x)
            {
                Log.Error(x.ToString());
            }
        }
        public static void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            // Process event. 
            Console.WriteLine("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }
    }
}
