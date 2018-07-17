using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.hitokoto.statusReport.Drivers.Hitokoto {
    public class hitokotoStatusBody {
        public class Rootobject {
            public string version { get; set; }
            public string[] children { get; set; }
            public Downserver[] downServer { get; set; }
            public Status status { get; set; }
            public Requests requests { get; set; }
            public long lastUpdate { get; set; }
            public string now { get; set; }
            public long ts { get; set; }
        }

        public class Status {
            public float[] load { get; set; }
            public float memory { get; set; }
            public Hitokoto hitokoto { get; set; }
            public Childstatu[] childStatus { get; set; }
        }

        public class Hitokoto {
            public int total { get; set; }
            public string[] categroy { get; set; }
        }

        public class Childstatu {
            public string id { get; set; }
            public Memory memory { get; set; }
            public float[] load { get; set; }
            public Hitokto hitokto { get; set; }
        }

        public class Memory {
            public float totol { get; set; }
            public float free { get; set; }
            public float usage { get; set; }
        }

        public class Hitokto {
            public int total { get; set; }
            public string[] categroy { get; set; }
            public long lastUpdate { get; set; }
        }

        public class Requests {
            public All all { get; set; }
            public Hosts hosts { get; set; }
        }

        public class All {
            public int total { get; set; }
            public int pastMinute { get; set; }
            public int pastHour { get; set; }
            public int pastDay { get; set; }
            public int[] dayMap { get; set; }
            public int[] FiveMinuteMap { get; set; }
        }

        public class Hosts {
            public V1HitokotoCn v1hitokotocn { get; set; }
            public ApiHitokotoCn apihitokotocn { get; set; }
            public SslapiHitokotoCn sslapihitokotocn { get; set; }
        }

        public class V1HitokotoCn {
            public int total { get; set; }
            public int pastMinute { get; set; }
            public int pastHour { get; set; }
            public int pastDay { get; set; }
            public int[] dayMap { get; set; }
        }

        public class ApiHitokotoCn {
            public int total { get; set; }
            public int pastMinute { get; set; }
            public int pastHour { get; set; }
            public int pastDay { get; set; }
            public int[] dayMap { get; set; }
        }

        public class SslapiHitokotoCn {
            public int total { get; set; }
            public int pastMinute { get; set; }
            public int pastHour { get; set; }
            public int pastDay { get; set; }
            public int[] dayMap { get; set; }
        }

        public class Downserver {
            public string id { get; set; }
            public long startTs { get; set; }
            public int last { get; set; }
            public Statusmessage statusMessage { get; set; }
        }

        public class Statusmessage {
            public bool isError { get; set; }
            public string id { get; set; }
            public int code { get; set; }
            public string msg { get; set; }
            public string stack { get; set; }
            public long ts { get; set; }
        }

    }
}
