    namespace SirenaAPI.Contract.User.Request;

public record RegisterRequest(
    string GoogleTokenId,
    bool AcceptTerms);