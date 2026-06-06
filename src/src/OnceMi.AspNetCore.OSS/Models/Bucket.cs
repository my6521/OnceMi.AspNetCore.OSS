namespace OnceMi.AspNetCore.OSS
{
    public class Bucket
    {
        public string Location { get; internal set; }

        public string Name { get; internal set; }

        public Owner Owner { get; internal set; }

        public string CreationDate { get; set; }
    }
}