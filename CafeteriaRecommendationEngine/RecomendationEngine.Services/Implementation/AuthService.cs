using Microsoft.EntityFrameworkCore;
using RecommendationEngine.DAL.DbConnection;
using RecommendationEngine.DataModel.Models;
using RecomendationEngine.Services.Interfaces;
using System.Threading.Tasks;
using RecommendationEngine.DAL.Repositories.Interfaces;
using RecommendationEngine.DAL.Repositories.Implementation;
using RecommendationEngine.Common.Exceptions;

namespace RecomendationEngine.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository authRepository;

        public AuthService(IAuthRepository authRepository)
        {
            this.authRepository = authRepository;
        }

        public async Task<User> Authenticate(string username, string password)
        {
            try
            {
                var user = await authRepository.Authenticate(username);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                if (!string.Equals(user.Password, password))
                {
                    throw new IncorrectPasswordException();
                }

                return user;
            }
            catch (UserNotFoundException)
            {
                // Handle user not found case if needed
                throw;
            }
            catch (IncorrectPasswordException)
            {
                // Handle incorrect password case if needed
                throw;
            }
            catch (Exception ex)
            {
                // Handle other potential exceptions
                throw new Exception("An error occurred during authentication.", ex);
            }
        }

    }
}
