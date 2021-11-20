namespace SingleSignOn.Admin.WebApi.Dtos.Users
{
    public class UserRoleApiDto<TKey>
    {
        public TKey UserId { get; set; }

        public TKey RoleId { get; set; }
    }
}







