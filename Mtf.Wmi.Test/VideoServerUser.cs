namespace Mtf.Wmi.Test
{
    public class VideoServerUser
    {
        public string Id { get; set; }

        public string FullName { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, Full name: {FullName}, Username: {Username}, Password hash: {PasswordHash}";
        }
    }
}
