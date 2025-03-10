using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PizzaShop.Repository.Interfaces;
using PizzaShop.Repository.Models;
using PizzaShop.Repository.ModelView;
using PizzaShop.Service.Interfaces;

namespace PizzaShop.Service.Implementations;

public class MenuService : IMenuService
{

    private readonly IGenericRepository<Category> _category;
    private readonly IGenericRepository<Item> _items;
    private readonly IGenericRepository<Modifiergroup> _modifierGroup;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MenuService(
        IGenericRepository<Category> category,
        IGenericRepository<Item> items,
        IGenericRepository<Modifiergroup> modifierGroup,
        IWebHostEnvironment webHostEnvironment)
    {
        _category = category;
        _items = items;
        _webHostEnvironment = webHostEnvironment;
        _modifierGroup = modifierGroup;
    }

    public async Task<MenuWithItemsViewModel> GetAllCategory(int? categoryId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 5)
    {
        List<Category>? categories = await _category.GetAllCategoryAsync();
        List<Item>? items;

        // Filter by category first
        if (categoryId.HasValue)
        {
            items = await _items.GetItemsByCategoryAsync(categoryId.Value);
        }
        else
        {
            items = await _items.GetAllItemsAsync();
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            items = items.Where(i => i.Itemname.ToLower().Contains(searchTerm)).ToList();
        }

        // Get total count before pagination
        int totalItems = items.Count;

        // Apply pagination
        items = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new MenuWithItemsViewModel
        {
            Categories = categories,
            Items = items,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems // Ensure totalItems is returned properly
        };
    }



    public async Task<List<Modifiergroup>> GetAllModifier(int? modifierId = null, string? searchModifier = null)
    {
        List<Modifiergroup>? mg = await _category.GetAllModifierGroupAsync();
        // List<Item>? items = await _items.GetAllItemsAsync();

        // Filter items by category if specified
        // if (modifierId.HasValue)
        // {
        //     items = _items.GetItemsByCategoryAsync(categoryId.Value).Result;
        // }

        // Apply search filter if a search term is provided
        // if (!string.IsNullOrEmpty(searchModifier))
        // {
        //     searchModifier = searchModifier.ToLower();
        //     items = items.Where(i => i.Itemname.ToLower().Contains(searchModifier)).ToList();
        // }

        return mg;
    }

    public async Task AddCategoryService(MenuWithItemsViewModel model)
    {
        Category category = new Category
        {
            Categorydescription = model.CategoryDescription,
            Categoryname = model.CategoryName,
            Createdat = DateTime.Now,
            Editedat = DateTime.Now,
            Createdbyid = model.Userid,
            Editedbyid = model.Userid
        };
        await _category.AddAsync(category);

    }
    public async Task AddModifierGroupService(MenuWithItemsViewModel model)
    {
        Modifiergroup modifiergroup = new Modifiergroup
        {
            Modifiergroupdescription = model.Modifiergroupdescription,
            Modifiergroupname = model.Modifiergroupname,
            Createdat = DateTime.Now,
            Editedat = DateTime.Now,
            Createdbyid = model.Userid,
            Editedbyid = model.Userid
        };
        await _modifierGroup.AddAsync(modifiergroup);

    }


    public async Task EditCategoryService(MenuWithItemsViewModel model)
    {
        Category? category = await _category.GetByIdAsync(model.Categoryid);
        if (category != null)
        {
            category.Categorydescription = model.CategoryDescription;
            category.Categoryname = model.CategoryName;
            category.Editedat = DateTime.Now;
            category.Editedbyid = model.Userid;
            await _category.UpdateAsync(category);
        }

    }

    public async Task DeleteCategoryService(MenuWithItemsViewModel model)
    {
        Category? category = await _category.GetByIdAsync(model.Categoryid);
        List<Item> items = await _items.GetAllItemsAsync();
        if (category != null)
        {
            category.Isdeleted = true;
            category.Deletedat = DateTime.Now;
            category.Deletedbyid = model.Userid;
            await _category.UpdateAsync(category);
        }
        foreach (var i in items)
        {
            if (i.Categoryid == category.Categoryid)
            {
                i.Isdeleted = true;
                i.Deletedat = DateTime.Now;
                i.Deletedbyid = model.Userid;
                await _items.UpdateAsync(i);
            }
        }

    }
    public async Task DeleteModifierGroupService(MenuWithItemsViewModel model)
    {
        Modifiergroup? mg = await _modifierGroup.GetByIdAsync(model.Modifiergroupid);
        // List<Item> items = await _items.GetAllItemsAsync();
        if (mg != null)
        {
            mg.Isdeleted = true;
            mg.Deletedat = DateTime.Now;
            mg.Deletedbyid = model.Userid;
            await _modifierGroup.UpdateAsync(mg);
        }
        // foreach (var i in items)
        // {
        //     if (i.Categoryid == category.Categoryid)
        //     {
        //         i.Isdeleted = true;
        //         i.Deletedat = DateTime.Now;
        //         i.Deletedbyid = model.Userid;
        //         await _items.UpdateAsync(i);
        //     }
        // }

    }
    public async Task DeleteItemService(MenuWithItemsViewModel model)
    {
        Item? item = await _items.GetByIdAsync(model.itemid);
        if (item != null)
        {
            item.Isdeleted = true;
            item.Deletedat = DateTime.Now;
            item.Deletedbyid = model.Userid;
            await _items.UpdateAsync(item);
        }
    }

    public async Task AddItemAsync(MenuWithItemsViewModel viewModel, IFormFile? uploadFile, int userId)
    {
        try
        {
            var item = new Item
            {
                Itemname = viewModel.item?.Itemname,
                Categoryid = viewModel.item.Categoryid,
                Itemtype = viewModel.item?.Itemtype,
                Rate = viewModel.item?.Rate,
                Quantity = viewModel.item?.Quantity,
                Units = viewModel.item?.Units,
                Isavailabe = viewModel.item?.Isavailabe,
                DefaultTax = viewModel.item.Defaulttax,
                Taxpercentage = viewModel.item?.Taxpercentage,
                Shortcode = viewModel.item?.Shortcode,
                Description = viewModel.item?.Description,
                Status = 1
            };

            if (uploadFile != null && uploadFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadFile.FileName);
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "items");
                Directory.CreateDirectory(uploadFolder);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await uploadFile.CopyToAsync(fileStream);
                }

                item.Imageid = "/items/" + uniqueFileName;
            }
            await _items.AddAsync(item);

        }
        catch (Exception ex)
        {
            throw new Exception("Error adding item: " + ex.Message, ex);
        }

    }

    public async Task<Item> GetItemById(int id)
    {
        return await _items.GetByIdAsync(id);
    }

    public async Task UpdateItemAsync(MenuWithItemsViewModel viewModel, IFormFile? uploadFile, int userId)
    {
        try
        {
            if (viewModel.item == null)
            {
                throw new ArgumentNullException(nameof(viewModel.item), "Item details are required.");
            }

            var item = await _items.GetByIdAsync(viewModel.itemid);
            if (item == null)
            {
                throw new Exception("Item not found.");
            }

            // Update item properties
            item.Categoryid = viewModel.item.Categoryid;
            item.Itemname = viewModel.item.Itemname;
            item.Itemtype = viewModel.item.Itemtype;
            item.Rate = viewModel.item.Rate;
            item.Quantity = viewModel.item.Quantity;
            item.Units = viewModel.item.Units;
            item.Isavailabe = viewModel.item.Isavailabe;
            item.DefaultTax = viewModel.item.Defaulttax;
            item.Taxpercentage = viewModel.item.Taxpercentage;
            item.Shortcode = viewModel.item.Shortcode;
            item.Description = viewModel.item.Description;
            item.Editedbyid = userId;
            item.Editedat = DateTime.Now;

            // Handle file upload
            if (uploadFile != null && uploadFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadFile.FileName);
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "items");
                Directory.CreateDirectory(uploadFolder);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await uploadFile.CopyToAsync(fileStream);
                }

                item.Imageid = "/items/" + uniqueFileName;
            }
            else
            {
                item.Imageid = item.Imageid;
            }

            string? result = await _items.UpdateAsync(item);
            System.Console.WriteLine("result: " + result);
        }
        catch (Exception ex)
        {
            throw new Exception("Error adding item: " + ex.Message, ex);
        }

    }


}
