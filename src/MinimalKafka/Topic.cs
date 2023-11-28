using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;
public class Topic
{
    public string TopicName { get; set; }
    public Delegate TopicHandler { get; internal set; }
}
