using System;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PizzaShop.Core.Filters;
using PizzaShop.Repository.Models;
using PizzaShop.Repository.ModelView;
using PizzaShop.Service.Interfaces;

namespace PizzaShop.Core.Controllers;

[Authorize]
[ServiceFilter(typeof(AuthorizePermissionMenu))]
public class MenuController : Controller
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly IUserTableService _userTableService;
    private readonly IRoleService _roleService;
    private readonly IMenuService _menuService;

    public MenuController(
        IUserTableService userTableService,
        IConfiguration configuration,
        IUserService userService,
        IRoleService roleService,
        IMenuService menuService)
    {
        _configuration = configuration;
        _userService = userService;
        _userTableService = userTableService;
        _roleService = roleService;
        _menuService = menuService;
    }

    public async Task FetchData()
    {
        string email = HttpContext.Items["UserEmail"] as string ?? string.Empty;
        string role = HttpContext.Items["UserRole"] as string ?? string.Empty;
        UserBagViewModel? userBag = await _userService.UserDetailBag(email);
        List<RolePermissionModelView>? rolefilter = await _userService.RoleFilter(role);
        if (userBag != null)
        {
            ViewBag.Username = userBag.UserName;
            ViewBag.Userid = userBag.UserId;
            ViewBag.ImageUrl = userBag.ImageUrl;
            ViewBag.permission = rolefilter;
        }
    }

    public async Task<IActionResult> Index(int? categoryId = null, string? searchTerm = null, int? modifierId = null, string? searchModifier = null, int pageNumber = 1, int pageSize = 5)
    {
        await FetchData();
        MenuWithItemsViewModel menu = await _menuService.GetAllCategory(categoryId, searchTerm, pageNumber, pageSize);
        MenuWithItemsViewModel menu2 = await _menuService.GetModifiers(modifierId, searchTerm, pageNumber, pageSize);
        ViewBag.SelectedCategoryId = categoryId;
        MenuWithItemsViewModel result = new MenuWithItemsViewModel
        {
            Categories = menu.Categories,
            Items = menu.Items,
            CurrentPage = menu.CurrentPage,
            PageSize = menu.PageSize,
            TotalItems = menu.TotalItems,
            modifiergroups = menu2.modifiergroups,
            Modifiers = menu2.Modifiers,
            CurrentPage1 = menu2.CurrentPage1,
            PageSize1 = menu2.PageSize1,
            TotalItems1 = menu2.TotalItems1
        };
        ViewBag.SelectedModifierId = modifierId;

        return View(result);
    }

    public async Task<IActionResult> FilterItems(int? categoryId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 5)
    {
        await FetchData();
        MenuWithItemsViewModel menu = await _menuService.GetAllCategory(categoryId, searchTerm, pageNumber, pageSize);
        return PartialView("_ItemsPartial", menu);
    }

    public async Task<IActionResult> FilterModifiers(int? modifierGroupId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 5)
    {
        await FetchData();
        MenuWithItemsViewModel menu = await _menuService.GetModifiers(modifierGroupId, searchTerm, pageNumber, pageSize);
        return PartialView("_ModifiersPartial", menu);
    }


    [HttpPost]
    public async Task<IActionResult> AddCategory(MenuWithItemsViewModel model)
    {
        await _menuService.AddCategoryService(model);
        await FetchData();
        TempData["CategoryAdd"] = "Category added successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AddModifierGroup(MenuWithItemsViewModel model)
    {
        await _menuService.AddModifierGroupService(model);
        await FetchData();
        TempData["ModifierGroupAdd"] = "ModifierGroup added successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(MenuWithItemsViewModel model)
    {
        await _menuService.EditCategoryService(model);
        await FetchData();
        TempData["CategoryAdd"] = "Category Edited successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(MenuWithItemsViewModel model)
    {
        await _menuService.DeleteCategoryService(model);
        await FetchData();
        TempData["CategoryAdd"] = "Modifier Group Deleted successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteModifierGroup(MenuWithItemsViewModel model)
    {
        await _menuService.DeleteModifierGroupService(model);
        await FetchData();
        TempData["ModifierGroupAdd"] = "Category Deleted successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(MenuWithItemsViewModel viewModel)
    {
        if (viewModel.item == null)
        {
            // Handle null item case early
            MenuWithItemsViewModel menu2 = await _menuService.GetAllCategory(0, "", 1, 5);
            menu2.item = viewModel.item;
            await FetchData();
            ModelState.AddModelError("", "Item details are required.");
            return View("Index", menu2);
        }
        if (viewModel.item != null)
        {
            try
            {
                await FetchData();
                await _menuService.AddItemAsync(viewModel, viewModel.item?.UploadFiles, ViewBag.Userid);

                TempData["SuccessMessage"] = "Item added successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("error :" + ex.Message);
                MenuWithItemsViewModel menu1 = await _menuService.GetAllCategory(0, "", 1, 5);
                menu1.item = viewModel.item;
                return RedirectToAction("Index", menu1);
            }
        }
        MenuWithItemsViewModel menu = await _menuService.GetAllCategory(0, "", 1, 5);
        menu.item = viewModel.item;
        return RedirectToAction("Index", menu);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModifier(MenuWithItemsViewModel viewModel)
    {
        if (viewModel.modifiersViewModel == null)
        {
            MenuWithItemsViewModel menu2 = await _menuService.GetModifiers(0, "", 1, 5);
            menu2.modifiersViewModel = viewModel.modifiersViewModel;
            ModelState.AddModelError("", "Item details are required.");
            return View("Index", menu2);
        }
        if (viewModel.modifiersViewModel != null)
        {
            try
            {
                await FetchData();
                await _menuService.AddModifierAsync(viewModel, ViewBag.Userid);
            }
            catch (Exception ex)
            {

                MenuWithItemsViewModel menu2 = await _menuService.GetModifiers(0, "", 1, 5);
                menu2.modifiersViewModel = viewModel.modifiersViewModel;
                ModelState.AddModelError("", "Item details are required.");
                return View("Index", menu2);
            }
        }
        return RedirectToAction("Index");
    }


    [HttpPost]
    public async Task<IActionResult> DeleteItem(MenuWithItemsViewModel model)
    {
        await _menuService.DeleteItemService(model);
        TempData["CategoryAdd"] = "Item Deleted successfully";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> EditItem(int id)
    {
        var item = await _menuService.GetItemById(id); // New service method
        if (item == null)
        {
            return NotFound();
        }

        var viewModel = new MenuWithItemsViewModel
        {
            item = new ItemsViewModel
            {
                Itemid = item.Itemid,
                Categoryid = item.Categoryid,
                Itemname = item.Itemname,
                Itemtype = item.Itemtype,
                Rate = item.Rate,
                Quantity = item.Quantity,
                Units = item.Units,
                Isavailabe = (bool)item.Isavailabe,
                Defaulttax = (bool)item.DefaultTax,
                Taxpercentage = item.Taxpercentage,
                Shortcode = item.Shortcode,
                Description = item.Description
            },
            // Fetch categories for dropdown
        };

        return View("Index", viewModel); // Reuse Index view with pre-filled modal
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItem(MenuWithItemsViewModel viewModel)
    {
        // Early check for null item
        if (viewModel.item == null)
        {
            MenuWithItemsViewModel menu = await _menuService.GetAllCategory(0, "", 1, 5);
            ModelState.AddModelError("", "Item details are required.");
            return View("Index", menu);
        }

        try
        {
            await FetchData();
            int userId = ViewBag.Userid;
            await _menuService.UpdateItemAsync(viewModel, viewModel.item.UploadFiles, userId);
            TempData["SuccessMessage"] = "Item updated successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("error :" + ex.Message);
            MenuWithItemsViewModel menu1 = await _menuService.GetAllCategory(0, "", 1, 5);
            menu1.item = viewModel.item;
            return RedirectToAction("Index", menu1);
        }
    }

    [HttpPost]
    public async Task<IActionResult> IsAvailableUpdate(int itemId, bool available)
    {
        try
        {
            await FetchData();
            int userId = ViewBag.Userid; // Consider dependency injection instead
            Item? item = await _menuService.IsAvailabeUpdateAsync(itemId, available, userId);

            if (item?.Isavailabe == available) // Added null check
            {
                return Json(new
                {
                    success = true,
                    data = "Update completed successfully"
                });
            }

            return Json(new
            {
                success = false,
                data = "Update failed to apply"
            });
        }
        catch (Exception e)
        {
            System.Console.WriteLine("Error in IsAvailableUpdate: " + e.Message);
            return Json(new
            {
                success = false,
                data = "An error occurred: " + e.Message
            });
        }
    }

}