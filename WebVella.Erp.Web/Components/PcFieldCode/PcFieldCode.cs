﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebVella.Erp.Exceptions;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Web.Services;
using WebVella.TagHelpers.Models;

namespace WebVella.Erp.Web.Components
{
	[PageComponent(Label = "Field Code", Library = "WebVella", Description = "A code editor", Version = "0.0.1", IconClass = "fas fa-code")]
	public class PcFieldCode : PcFieldBase
	{
		protected ErpRequestContext ErpRequestContext { get; set; }

		public PcFieldCode([FromServices]ErpRequestContext coreReqCtx)
		{
			ErpRequestContext = coreReqCtx;
		}

		public class PcFieldCodeOptions : PcFieldBaseOptions
		{
			[JsonProperty(PropertyName = "height")]
			public string Height { get; set; } = "120px";

			[JsonProperty(PropertyName = "language")]
			public string Language { get; set; } = "razor";

			[JsonProperty(PropertyName = "theme")]
			public string Theme { get; set; } = "cobalt";

			public static PcFieldCodeOptions CopyFromBaseOptions(PcFieldBaseOptions input)
			{
				return new PcFieldCodeOptions
				{
					IsVisible = input.IsVisible,
					LabelMode = input.LabelMode,
					LabelText = input.LabelText,
					Mode = input.Mode,
					Name = input.Name,
					Height = "120px",
					Language = "razor",
					Theme="cobalt"
				};
			}
		}

		public async Task<IViewComponentResult> InvokeAsync(PageComponentContext context)
		{
			ErpPage currentPage = null;
			try
			{
				#region << Init >>
				if (context.Node == null)
				{
					return await Task.FromResult<IViewComponentResult>(Content("Error: The node Id is required to be set as query parameter 'nid', when requesting this component"));
				}


				var pageFromModel = context.DataModel.GetProperty("Page");
				if (pageFromModel is ErpPage)
				{
					currentPage = (ErpPage)pageFromModel;
				}
				else
				{
					return await Task.FromResult<IViewComponentResult>(Content("Error: PageModel does not have Page property or it is not from ErpPage Type"));
				}

				if (currentPage == null)
				{
					return await Task.FromResult<IViewComponentResult>(Content("Error: The page Id is required to be set as query parameter 'pid', when requesting this component"));
				}

				var baseOptions = InitPcFieldBaseOptions(context);
				var options = PcFieldCodeOptions.CopyFromBaseOptions(baseOptions);
				if (context.Options != null)
				{
					options = JsonConvert.DeserializeObject<PcFieldCodeOptions>(context.Options.ToString());
				}
				var modelFieldLabel = "";
				var model = (PcFieldBaseModel)InitPcFieldBaseModel(context, options, label: out modelFieldLabel);
				if (String.IsNullOrWhiteSpace(options.LabelText) && context.Mode != ComponentMode.Options)
				{
					options.LabelText = modelFieldLabel;
				}


				ViewBag.LabelMode = options.LabelMode;
				ViewBag.Mode = options.Mode;

				if (options.LabelMode == WvLabelRenderMode.Undefined && baseOptions.LabelMode != WvLabelRenderMode.Undefined)
					ViewBag.LabelMode = baseOptions.LabelMode;

				if (options.Mode == WvFieldRenderMode.Undefined && baseOptions.Mode != WvFieldRenderMode.Undefined)
					ViewBag.Mode = baseOptions.Mode;

				var componentMeta = new PageComponentLibraryService().GetComponentMeta(context.Node.ComponentName);

				var accessOverride = context.DataModel.GetPropertyValueByDataSource(options.AccessOverrideDs) as WvFieldAccess?;
				if(accessOverride != null){
					model.Access = accessOverride.Value;
				}
				var requiredOverride = context.DataModel.GetPropertyValueByDataSource(options.RequiredOverrideDs) as bool?;
				if(requiredOverride != null){
					model.Required = requiredOverride.Value;
				}
				else{
					if(!String.IsNullOrWhiteSpace(options.RequiredOverrideDs)){
						if(options.RequiredOverrideDs.ToLowerInvariant() == "true"){
							model.Required = true;
						}
						else if(options.RequiredOverrideDs.ToLowerInvariant() == "false"){
							model.Required = false;
						}
					}
				}
				#endregion

				ViewBag.Options = options;
				ViewBag.Model = model;
				ViewBag.Node = context.Node;
				ViewBag.ComponentMeta = componentMeta;
				ViewBag.RequestContext = ErpRequestContext;
				ViewBag.AppContext = ErpAppContext.Current;

				if (context.Mode != ComponentMode.Options && context.Mode != ComponentMode.Help)
				{
					var isVisible = true;
					var isVisibleDS = context.DataModel.GetPropertyValueByDataSource(options.IsVisible);
					if (isVisibleDS is string && !String.IsNullOrWhiteSpace(isVisibleDS.ToString()))
					{
						if (Boolean.TryParse(isVisibleDS.ToString(), out bool outBool))
						{
							isVisible = outBool;
						}
					}
					else if (isVisibleDS is Boolean)
					{
						isVisible = (bool)isVisibleDS;
					}
					ViewBag.IsVisible = isVisible;

					model.Value = context.DataModel.GetPropertyValueByDataSource(options.Value);
				}

				switch (context.Mode)
				{
					case ComponentMode.Display:
						return await Task.FromResult<IViewComponentResult>(View("Display"));
					case ComponentMode.Design:
						return await Task.FromResult<IViewComponentResult>(View("Design"));
					case ComponentMode.Options:
						return await Task.FromResult<IViewComponentResult>(View("Options"));
					case ComponentMode.Help:
						return await Task.FromResult<IViewComponentResult>(View("Help"));
					default:
						ViewBag.Error = new ValidationException()
						{
							Message = "Unknown component mode"
						};
						return await Task.FromResult<IViewComponentResult>(View("Error"));
				}

			}
			catch (ValidationException ex)
			{
				ViewBag.Error = ex;
				return await Task.FromResult<IViewComponentResult>(View("Error"));
			}
			catch (Exception ex)
			{
				ViewBag.Error = new ValidationException()
				{
					Message = ex.Message
				};
				return await Task.FromResult<IViewComponentResult>(View("Error"));
			}
		}
	}
}
