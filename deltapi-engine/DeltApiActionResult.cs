using System.Net;
using System.Xml.Serialization;

namespace deltapi_engine;

public class DeltApiActionResult
{
    public TimeSpan Duration { get; set; }
    public HttpStatusCode? StatusCode { get; set; }
    
    [XmlIgnore]
    public dynamic Content { get; set; }
    
}