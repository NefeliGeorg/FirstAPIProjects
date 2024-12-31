using QBM.CompositionApi.ApiManager;
using QBM.CompositionApi.Definition;

namespace QBM.CompositionApi.Sdk01_Basics {
    public class CustomMethod : IApiProviderFor <QER.CompositionApi.Portal.PortalApiProject>, IApiProvider {
        public void Build(IApiBuilder builder) {
            // This is how a method can return objects of any type,
            // provided that the type can be serialized.
            builder.AddMethod(Method.Define("helloworld")
                .AllowUnauthenticated()
                .HandleGet(qr => new DataObject { Message = "Hello world!" }));

            // This is how posted data of any type can be processed.
            builder.AddMethod(Method.Define("helloworld/post")
                .AllowUnauthenticated()
                .Handle<PostedMessage, DataObject>("POST", (posted, qr) => new DataObject {
                    Message = "You posted the following message: " + posted.Input
                }));
            // This is an example of a method that generates plain text (not JSON formatted).
            // You can use this to generate content of any type.
            builder.AddMethod(Method.Define("helloworld/text")
                .AllowUnauthenticated()
                .HandleGet(new ContentTypeSelector
                {
                    // Specifiy the MIME type of the response.
                    new ResponseBuilder("text/plain", async (qr, ct) => {
                        return new System.Net.Http.HttpResponseMessage
                        {
                            Content = new System.Net.Http.StringContent("Hello world!")
                        };
                    })
                }, typeof(string)));
        }
    }


    // This class defines the type of data object that will be sent to the client.
    public class DataObject {
        public string Message { get; set; }
    }

    // This class defines the type of data object that will be sent from the client to the server.
    public class PostedMessage {
        public string Input { get; set; }
    }
}
