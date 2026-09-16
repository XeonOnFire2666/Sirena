using SirenaAPI.Logic.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SirenaAPI.Logic.Contract.User.Response
{
    public record TokensResponse(
        RefreshToken? RefreshToken = null,
        string? AccessToken = null);
}
