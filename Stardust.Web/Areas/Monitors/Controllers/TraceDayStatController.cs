﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Charts;
using NewLife.Data;
using NewLife.Web;
using Stardust.Data.Monitors;
using XCode.Membership;
using static Stardust.Data.Monitors.TraceDayStat;

namespace Stardust.Web.Areas.Monitors.Controllers
{
    [MonitorsArea]
    public class TraceDayStatController : EntityController<TraceDayStat>
    {
        static TraceDayStatController()
        {
            MenuOrder = 70;
        }

        protected override IEnumerable<TraceDayStat> Search(Pager p)
        {
            var appId = p["appId"].ToInt(-1);
            var name = p["name"];

            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            // 选了应用，没有选时间，按照统计日期升序
            if (appId >= 0 && start.Year < 2000 && p.Sort.IsNullOrEmpty())
            {
                start = DateTime.Today.AddDays(-30);
                p["dtStart"] = start.ToString("yyyy-MM-dd");

                p.Sort = __.StatDate;
                p.Desc = false;
                p.PageSize = 100;
            }
            // 选了应用和时间，按照接口调用次数降序
            else if (appId >= 0 && start.Year > 2000 && p.Sort.IsNullOrEmpty())
            {
                p.Sort = __.Total;
                p.Desc = true;
                p.PageSize = 100;
            }

            p.RetrieveState = true;

            var list = TraceDayStat.Search(appId, name, start, end, p["Q"], p);

            if (list.Count > 0 && !name.IsNullOrEmpty())
            {
                // 绘制日期曲线图
                var ar = AppTracer.FindByID(appId);
                if (appId >= 0)
                {
                    var chart = new ECharts
                    {
                        Height = 400,
                    };
                    chart.SetX(list, _.StatDate, e => e.StatDate.ToString("MM-dd"));
                    chart.SetY("调用次数");
                    chart.AddLine(list, _.Total, null, true);
                    chart.Add(list, _.Errors);
                    chart.SetTooltip();
                    ViewBag.Charts = new[] { chart };
                }
                if (appId >= 0)
                {
                    var chart = new ECharts
                    {
                        Height = 400,
                    };
                    chart.SetX(list, _.StatDate, e => e.StatDate.ToString("MM-dd"));
                    chart.SetY("耗时");
                    chart.AddLine(list, _.Cost, null, true);
                    chart.Add(list, _.MaxCost);
                    chart.Add(list, _.MinCost);
                    chart.SetTooltip();
                    ViewBag.Charts2 = new[] { chart };
                }
            }

            return list;
        }

        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Trace(Int32 id)
        {
            var st = TraceDayStat.FindByID(id);
            if (st == null) throw new InvalidDataException("找不到采样数据");

            var ds = TraceData.Search(st.AppId, st.Name, st.StatDate, st.StatDate, null, new PageParameter { PageSize = 1 });
            if (ds.Count == 0) throw new InvalidDataException("找不到采样数据");

            var list = SampleData.FindAllByDataId(ds[0].ID);
            if (list.Count == 0) throw new InvalidDataException("找不到采样数据");

            return RedirectToAction("Index", "SampleData", new { traceId = list[0].TraceId });
        }
    }
}