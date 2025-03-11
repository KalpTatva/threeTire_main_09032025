using Microsoft.EntityFrameworkCore;
using PizzaShop.Repository.Interfaces;
using PizzaShop.Repository.Models;
using PizzaShop.Repository.ModelView;

namespace PizzaShop.Repository.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly PizzaShopContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(PizzaShopContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<Account?> GetAccountByEmail(string email)
        {
            return await _dbSet.OfType<Account>().FirstOrDefaultAsync(u => u.Email == email) ?? null;

        }

        public async Task<string?> UpdateAsync(T model)
        {
            if (model != null)
            {
                _dbSet.Update(model);
                await _context.SaveChangesAsync();
                return "saved";
            }
            return "unSaved";
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _dbSet.OfType<User>().FirstOrDefaultAsync(u => u.Accountid == id);
        }
        public async Task<Image?> GetImageByIdAsync(int id)
        {
            return await _dbSet.OfType<Image>().FirstOrDefaultAsync(u => u.Userid == id);
        }

        public async Task AddImageAsync(Image image)
        {
            _context.Add(image);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RolePermissionModelView>?> GetRolePermissionModelViewAsync(int roleid)
        {

            List<RolePermissionModelView> result = (from pr in _context.PermissionsRoles
                                                    join r in _context.Roles on pr.Roleid equals r.Roleid
                                                    join p in _context.Permissions on pr.Permissionid equals p.Permissionid
                                                    where r.Roleid == roleid
                                                    select new RolePermissionModelView
                                                    {
                                                        RoleId = r.Roleid,
                                                        PermissionId = p.Permissionid,
                                                        Canview = (bool)pr.Canview,
                                                        Canedit = (bool)pr.Canedit,
                                                        Candelete = (bool)pr.Candelete
                                                    }).ToList();
            return result;
        }

        public async Task<RolePermissionModelView?> GetPermissionAsync(int roleid, int permission)
        {
            RolePermissionModelView? result =
            (
                from pr in _context.PermissionsRoles
                join r in _context.Roles on pr.Roleid equals r.Roleid
                join p in _context.Permissions on pr.Permissionid equals p.Permissionid
                where r.Roleid == roleid && p.Permissionid == 1
                select new RolePermissionModelView
                {
                    RoleId = roleid,
                    PermissionId = p.Permissionid,
                    Canview = (bool)pr.Canview,
                    Canedit = (bool)pr.Canedit,
                    Candelete = (bool)pr.Candelete
                }
            ).FirstOrDefault();
            return result;
        }

        public async Task<List<UserTableViewModel>?> UserDetailAsync()
        {
            return await (from u in _context.Users
                          join i in _context.Images on u.Userid equals i.Userid into img
                          from i in img.DefaultIfEmpty() // Ensures all users are included even if no image exists
                          join a in _context.Accounts on u.Accountid equals a.Accountid
                          where u.Isdeleted == false && a.Isdeleted == false
                          orderby u.Userid descending
                          select new UserTableViewModel
                          {
                             AccountId = a.Accountid,
                              Id = u.Userid,
                              Firstname = u.Firstname,
                              Lastname = u.Lastname,
                              Email = a.Email,
                              Phone = u.Phone,
                              Role = a.Roleid,
                              Status = u.Status,
                              Image = i != null ? i.Imageurl : null // Handles null images properly
                          }).ToListAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int? id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<List<City>> GetCityListByIdAsync(int id)
        {
            return await _context.Cities.Where(b => b.Stateid == id).ToListAsync();
        }

        public async Task<List<State>> GetStateListByIdAsync(int id)
        {
            return await _context.States.Where(b => b.Countryid == id).ToListAsync();
        }

        public async Task AddAsync(T model)
        {
            _dbSet.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RolePermissionModelView>?> GetPermissionAsync(int id)
        {
            var result = await (from pr in _context.PermissionsRoles
                                join r in _context.Roles on pr.Roleid equals r.Roleid
                                join p in _context.Permissions on pr.Permissionid equals p.Permissionid
                                where r.Roleid == id orderby pr.Permissionroleid
                                select new RolePermissionModelView
                                {
                                    RoleId = id,
                                    PermissionId = p.Permissionid,
                                    RoleName = r.Rolename,
                                    PermissionName = p.Permissionname,
                                    Canview = (bool)pr.Canview,
                                    Canedit = (bool)pr.Canedit,
                                    Candelete = (bool)pr.Candelete
                                }).ToListAsync();
            return result;
        }

        public async Task<PermissionsRole?> GetRoleAndPermissionAsync(int roleid, int permissionid)
        {
            return await _dbSet.OfType<PermissionsRole>().FirstOrDefaultAsync(p => p.Roleid == roleid && p.Permissionid == permissionid);
        }

        public async Task<List<Category>> GetAllCategoryAsync()
        {
            return await _context.Categories.Where(u => u.Isdeleted == false).OrderBy(u => u.Categoryid).ToListAsync();
        }

        public async Task<List<Modifiergroup>> GetAllModifierGroupAsync()
        {
            return await _context.Modifiergroups.Where(u => u.Isdeleted == false).OrderBy(u => u.Modifiergroupid).ToListAsync();
        }

        public async Task<List<Item>> GetAllItemsAsync()
        {
            return await _context.Items.Where(u => u.Isdeleted == false).OrderBy(u => u.Itemid).ToListAsync();
        }
        public async Task<List<Modifier>> GetAllModifierAsync()
        {
            return await _context.Modifiers.Where(u => u.Isdeleted == false).OrderBy(u => u.Modifierid).ToListAsync();
        }

        public async Task<List<Item>> GetItemsByCategoryAsync(int? id)
        {
            return await _context.Items.Where(u => u.Isdeleted == false && u.Categoryid == id ).OrderBy(u => u.Itemid).ToListAsync();
        }
        public async Task<List<Modifier>> GetModifiersByMGAsync(int? id)
        {
            return await _context.Modifiers.Where(u => u.Isdeleted == false && u.Modifiergroupid == id ).OrderBy(u => u.Modifierid).ToListAsync();
        }

        
    }
}


