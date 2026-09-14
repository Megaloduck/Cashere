using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// Hashes and verifies cashier passwords. Implemented in Cashere.Data using
// PBKDF2 (Microsoft.AspNetCore.Cryptography.KeyDerivation) - kept as an
// interface in the shared project so nothing outside Cashere.Data needs to
// know which hashing algorithm is behind it, same split as every other
// service pair in this project.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}