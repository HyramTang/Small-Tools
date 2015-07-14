using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DMES.Utility.DbProperties;

namespace DownTimeSplitService
{
    [DbEntity(TableName = "tbProductiveData", AutoIncFieldName = "Id", KeyFieldName = "Id")]
    [Serializable]
    public class ProductiveDataNew
    {
        public int Id { set; get; }
        public string THMID { set; get; }
        public int LINIENNR { set; get; }
        public string LINIENNAME { set; get; }
        public int STATIONNR { set; get; }
        public string STATIONNAME { set; get; }
        public string NAME { set; get; }
        public string WERT { set; get; }
        public DateTime TIME { set; get; }
    }
}
