using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Cashere.Models;

namespace Cashere.Services;

// Coarse "can this cashier modify admin data" check, now that login provides
// a real signed-in Cashier and Role. Deliberately a single view-vs-manage
// split rather than a permission-per-action matrix - Owner and Manager get
// full edit rights, Cashier is view-only throughout Admin. Screens call this
// rather than comparing UserRole directly so the split can be made
// finer-grained later (Owner-only actions, per-action limits, etc.) without
// hunting down every call site.
public static class RolePermissions
{
    public static bool CanManage(UserRole role) => role != UserRole.Cashier;
}