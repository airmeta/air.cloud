import { requestClient } from '#/api/request';

const LOG_BASE_URL = '/api/v1/logs';

/**
 * zh-cn: 接口开放平台的访问日志接口。请求路径、查询参数和返回字段与原开放平台保持一致，
 * 仅替换为当前管理端的请求客户端，以保留统一的 Ticket、AppId 和签名头。
 * en-us: Access-log endpoints for the Open platform. Paths, query parameters, and response
 * fields remain compatible with the original platform while using this admin client's headers.
 */
export const LogApi = {
  getPage(queryParams: LogApi.LogPageQuery) {
    return requestClient.get<LogApi.PageResult<LogApi.LogPageVO[]>>(
      `${LOG_BASE_URL}/page`,
      { params: queryParams },
    );
  },

  getVisitTrend(queryParams: LogApi.VisitTrendQuery) {
    return requestClient.get<LogApi.VisitTrendVO>(
      `${LOG_BASE_URL}/visit-trend`,
      { params: queryParams },
    );
  },

  getVisitStats() {
    return requestClient.get<LogApi.VisitStatsVO>(
      `${LOG_BASE_URL}/visit-stats`,
    );
  },
};

export namespace LogApi {
  export interface PageResult<T> {
    list: T;
    total: number;
  }

  export interface LogPageQuery {
    /** 搜索关键字 */
    keywords?: string;
    /** 操作时间 */
    createTime?: [string, string];
    /** 当前页码，继承原 PageQuery */
    pageNum: number;
    /** 每页条数，继承原 PageQuery */
    pageSize: number;
  }

  export interface LogPageVO {
    /** 主键 */
    id: string;
    /** 日志模块 */
    module: string;
    /** 日志内容 */
    content: string;
    /** 请求路径 */
    requestUri: string;
    /** 请求方法 */
    method: string;
    /** IP 地址 */
    ip: string;
    /** 地区 */
    region: string;
    /** 浏览器 */
    browser: string;
    /** 终端系统 */
    os: string;
    /** 执行时间（毫秒） */
    executionTime: number;
    /** 操作人 */
    operator: string;
  }

  export interface VisitTrendQuery {
    /** 开始日期 */
    startDate: string;
    /** 结束日期 */
    endDate: string;
  }

  export interface VisitTrendVO {
    /** 日期列表 */
    dates: string[];
    /** 浏览量（PV） */
    pvList: number[];
    /** 访客数（UV） */
    uvList: number[];
    /** IP 数 */
    ipList: number[];
  }

  export interface VisitStatsVO {
    /** 今日访客数（UV） */
    todayUvCount: number;
    /** 总访客数 */
    totalUvCount: number;
    /** 访客数同比增长率 */
    uvGrowthRate: number;
    /** 今日浏览量（PV） */
    todayPvCount: number;
    /** 总浏览量 */
    totalPvCount: number;
    /** 浏览量同比增长率 */
    pvGrowthRate: number;
  }
}

export default LogApi;
