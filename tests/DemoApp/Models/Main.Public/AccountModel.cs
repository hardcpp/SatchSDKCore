using SSC.DB;
using SSC.DB.Attributes;

namespace DemoApp.Models.Main.Public;

[DbTable("Main")]
internal class AccountModel : DbModel<AccountModel>
{
    [DbField]
    public long ID;
}
