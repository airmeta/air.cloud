export interface SignatureOptions {
  /**
   * zh-cn: 当前接入应用的 AppId，用于参与后端票据签名。
   * en-us: AppId of the current client application, included in the backend ticket signature.
   */
  appId?: string;
  /**
   * zh-cn: POST/PUT 请求体数据，存在有效内容时会序列化后参与签名。
   * en-us: POST/PUT request body data, serialized into the signature when meaningful content exists.
   */
  data?: unknown;
  /**
   * zh-cn: HTTP 方法名称，内部会统一转换为大写后判断签名来源。
   * en-us: HTTP method name, normalized to upper case before selecting the signature source.
   */
  method: string;
  /**
   * zh-cn: 本次请求随机串，用于防止重放并参与签名。
   * en-us: Per-request nonce used to prevent replay and included in the signature.
   */
  nonce: string;
  /**
   * zh-cn: GET/DELETE 查询参数，非空值会按键名大写后参与签名。
   * en-us: GET/DELETE query parameters; non-empty values are included with upper-cased keys.
   */
  params?: Record<string, any>;
  /**
   * zh-cn: 当前登录票据，SSO 认证链路只使用 Ticket。
   * en-us: Current login ticket; the SSO authentication flow uses Ticket only.
   */
  ticket?: string;
  /**
   * zh-cn: 请求时间戳，和 Nonce 一起组成防重放校验输入。
   * en-us: Request timestamp, paired with the nonce as anti-replay signature input.
   */
  timestamp: string;
  /**
   * zh-cn: 请求地址路径，必须与后端验签时使用的地址保持一致。
   * en-us: Request URL path, which must match the value used by backend signature validation.
   */
  url: string;
}

/**
 * zh-cn: 创建浏览器端请求随机串，优先使用 Web Crypto 的 randomUUID。
 * en-us: Creates a browser-side request nonce, preferring Web Crypto randomUUID when available.
 */
export function createUuid() {
  if (globalThis.crypto?.randomUUID) {
    return globalThis.crypto.randomUUID();
  }

  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const random = (Math.random() * 16) | 0;
    const value = char === 'x' ? random : (random & 0x3) | 0x8;
    return value.toString(16);
  });
}

/**
 * zh-cn: 根据 Air.Cloud 票据请求契约生成 MD5 大写签名。
 * en-us: Generates the upper-case MD5 signature required by the Air.Cloud ticket request contract.
 *
 * @param options
 * zh-cn: 请求方法、地址、AppId、Ticket、时间戳、随机串以及参数/请求体。
 * en-us: Request method, URL, AppId, Ticket, timestamp, nonce, and query/body payload.
 *
 * @returns
 * zh-cn: 后端验签使用的大写 MD5 签名字符串。
 * en-us: Upper-case MD5 signature string consumed by backend validation.
 */
export function createSignature(options: SignatureOptions) {
  const method = options.method.toUpperCase();
  const values: Record<string, string> = {
    APPID: options.appId || '',
    NONCE: options.nonce,
    TIMESTAMP: options.timestamp,
    URL: options.url,
  };

  if (options.ticket) {
    values.TICKET = options.ticket;
  }

  if (method === 'GET' || method === 'DELETE') {
    for (const [key, value] of Object.entries(options.params ?? {})) {
      if (value !== undefined && value !== null && value !== '') {
        values[key] = String(value);
      }
    }
  }

  if ((method === 'POST' || method === 'PUT') && options.data) {
    const payload = JSON.stringify(options.data);
    if (payload && payload !== '{}') {
      values.DATA = payload;
    }
  }

  const sorted: Record<string, string> = {};
  for (const key of Object.keys(values).sort()) {
    sorted[key] = values[key] ?? '';
  }

  return md5(JSON.stringify(sorted)).toUpperCase();
}

function md5(input: string) {
  const rotateLeft = (value: number, shift: number) =>
    (value << shift) | (value >>> (32 - shift));
  const addUnsigned = (x: number, y: number) => {
    const x4 = x & 0x4000_0000;
    const y4 = y & 0x4000_0000;
    const x8 = x & 0x8000_0000;
    const y8 = y & 0x8000_0000;
    const result = (x & 0x3fff_ffff) + (y & 0x3fff_ffff);
    if (x4 & y4) return result ^ 0x8000_0000 ^ x8 ^ y8;
    if (x4 | y4) {
      if (result & 0x4000_0000) return result ^ 0xc000_0000 ^ x8 ^ y8;
      return result ^ 0x4000_0000 ^ x8 ^ y8;
    }
    return result ^ x8 ^ y8;
  };
  const f = (x: number, y: number, z: number) => (x & y) | (~x & z);
  const g = (x: number, y: number, z: number) => (x & z) | (y & ~z);
  const h = (x: number, y: number, z: number) => x ^ y ^ z;
  const i = (x: number, y: number, z: number) => y ^ (x | ~z);
  const ff = (
    a: number,
    b: number,
    c: number,
    d: number,
    x: number,
    s: number,
    ac: number,
  ) => addUnsigned(rotateLeft(addUnsigned(addUnsigned(a, f(b, c, d)), addUnsigned(x, ac)), s), b);
  const gg = (
    a: number,
    b: number,
    c: number,
    d: number,
    x: number,
    s: number,
    ac: number,
  ) => addUnsigned(rotateLeft(addUnsigned(addUnsigned(a, g(b, c, d)), addUnsigned(x, ac)), s), b);
  const hh = (
    a: number,
    b: number,
    c: number,
    d: number,
    x: number,
    s: number,
    ac: number,
  ) => addUnsigned(rotateLeft(addUnsigned(addUnsigned(a, h(b, c, d)), addUnsigned(x, ac)), s), b);
  const ii = (
    a: number,
    b: number,
    c: number,
    d: number,
    x: number,
    s: number,
    ac: number,
  ) => addUnsigned(rotateLeft(addUnsigned(addUnsigned(a, i(b, c, d)), addUnsigned(x, ac)), s), b);
  const convertToWordArray = (text: string) => {
    const byteLength = text.length;
    const wordCount = (((byteLength + 8) >>> 6) + 1) * 16;
    const wordArray = new Array<number>(wordCount).fill(0);
    for (let index = 0; index < byteLength; index++) {
      const wordIndex = index >> 2;
      wordArray[wordIndex] =
        (wordArray[wordIndex] ?? 0) |
        (text.charCodeAt(index) << ((index % 4) * 8));
    }
    const finalWordIndex = byteLength >> 2;
    wordArray[finalWordIndex] =
      (wordArray[finalWordIndex] ?? 0) | (0x80 << ((byteLength % 4) * 8));
    wordArray[wordCount - 2] = byteLength << 3;
    wordArray[wordCount - 1] = byteLength >>> 29;
    return wordArray;
  };
  const utf8 = unescape(encodeURIComponent(input));
  const x = convertToWordArray(utf8);
  const word = (index: number) => x[index] ?? 0;
  let a = 0x6745_2301;
  let b = 0xefcd_ab89;
  let c = 0x98ba_dcfe;
  let d = 0x1032_5476;
  for (let k = 0; k < x.length; k += 16) {
    const aa = a;
    const bb = b;
    const cc = c;
    const dd = d;
    a = ff(a, b, c, d, word(k), 7, 0xd76a_a478);
    d = ff(d, a, b, c, word(k + 1), 12, 0xe8c7_b756);
    c = ff(c, d, a, b, word(k + 2), 17, 0x2420_70db);
    b = ff(b, c, d, a, word(k + 3), 22, 0xc1bd_ceee);
    a = ff(a, b, c, d, word(k + 4), 7, 0xf57c_0faf);
    d = ff(d, a, b, c, word(k + 5), 12, 0x4787_c62a);
    c = ff(c, d, a, b, word(k + 6), 17, 0xa830_4613);
    b = ff(b, c, d, a, word(k + 7), 22, 0xfd46_9501);
    a = ff(a, b, c, d, word(k + 8), 7, 0x6980_98d8);
    d = ff(d, a, b, c, word(k + 9), 12, 0x8b44_f7af);
    c = ff(c, d, a, b, word(k + 10), 17, 0xffff_5bb1);
    b = ff(b, c, d, a, word(k + 11), 22, 0x895c_d7be);
    a = ff(a, b, c, d, word(k + 12), 7, 0x6b90_1122);
    d = ff(d, a, b, c, word(k + 13), 12, 0xfd98_7193);
    c = ff(c, d, a, b, word(k + 14), 17, 0xa679_438e);
    b = ff(b, c, d, a, word(k + 15), 22, 0x49b4_0821);
    a = gg(a, b, c, d, word(k + 1), 5, 0xf61e_2562);
    d = gg(d, a, b, c, word(k + 6), 9, 0xc040_b340);
    c = gg(c, d, a, b, word(k + 11), 14, 0x265e_5a51);
    b = gg(b, c, d, a, word(k), 20, 0xe9b6_c7aa);
    a = gg(a, b, c, d, word(k + 5), 5, 0xd62f_105d);
    d = gg(d, a, b, c, word(k + 10), 9, 0x0244_1453);
    c = gg(c, d, a, b, word(k + 15), 14, 0xd8a1_e681);
    b = gg(b, c, d, a, word(k + 4), 20, 0xe7d3_fbc8);
    a = gg(a, b, c, d, word(k + 9), 5, 0x21e1_cde6);
    d = gg(d, a, b, c, word(k + 14), 9, 0xc337_07d6);
    c = gg(c, d, a, b, word(k + 3), 14, 0xf4d5_0d87);
    b = gg(b, c, d, a, word(k + 8), 20, 0x455a_14ed);
    a = gg(a, b, c, d, word(k + 13), 5, 0xa9e3_e905);
    d = gg(d, a, b, c, word(k + 2), 9, 0xfcef_a3f8);
    c = gg(c, d, a, b, word(k + 7), 14, 0x676f_02d9);
    b = gg(b, c, d, a, word(k + 12), 20, 0x8d2a_4c8a);
    a = hh(a, b, c, d, word(k + 5), 4, 0xfffa_3942);
    d = hh(d, a, b, c, word(k + 8), 11, 0x8771_f681);
    c = hh(c, d, a, b, word(k + 11), 16, 0x6d9d_6122);
    b = hh(b, c, d, a, word(k + 14), 23, 0xfde5_380c);
    a = hh(a, b, c, d, word(k + 1), 4, 0xa4be_ea44);
    d = hh(d, a, b, c, word(k + 4), 11, 0x4bde_cfa9);
    c = hh(c, d, a, b, word(k + 7), 16, 0xf6bb_4b60);
    b = hh(b, c, d, a, word(k + 10), 23, 0xbebf_bc70);
    a = hh(a, b, c, d, word(k + 13), 4, 0x289b_7ec6);
    d = hh(d, a, b, c, word(k), 11, 0xeaa1_27fa);
    c = hh(c, d, a, b, word(k + 3), 16, 0xd4ef_3085);
    b = hh(b, c, d, a, word(k + 6), 23, 0x0488_1d05);
    a = hh(a, b, c, d, word(k + 9), 4, 0xd9d4_d039);
    d = hh(d, a, b, c, word(k + 12), 11, 0xe6db_99e5);
    c = hh(c, d, a, b, word(k + 15), 16, 0x1fa2_7cf8);
    b = hh(b, c, d, a, word(k + 2), 23, 0xc4ac_5665);
    a = ii(a, b, c, d, word(k), 6, 0xf429_2244);
    d = ii(d, a, b, c, word(k + 7), 10, 0x432a_ff97);
    c = ii(c, d, a, b, word(k + 14), 15, 0xab94_23a7);
    b = ii(b, c, d, a, word(k + 5), 21, 0xfc93_a039);
    a = ii(a, b, c, d, word(k + 12), 6, 0x655b_59c3);
    d = ii(d, a, b, c, word(k + 3), 10, 0x8f0c_cc92);
    c = ii(c, d, a, b, word(k + 10), 15, 0xffef_f47d);
    b = ii(b, c, d, a, word(k + 1), 21, 0x8584_5dd1);
    a = ii(a, b, c, d, word(k + 8), 6, 0x6fa8_7e4f);
    d = ii(d, a, b, c, word(k + 15), 10, 0xfe2c_e6e0);
    c = ii(c, d, a, b, word(k + 6), 15, 0xa301_4314);
    b = ii(b, c, d, a, word(k + 13), 21, 0x4e08_11a1);
    a = ii(a, b, c, d, word(k + 4), 6, 0xf753_7e82);
    d = ii(d, a, b, c, word(k + 11), 10, 0xbd3a_f235);
    c = ii(c, d, a, b, word(k + 2), 15, 0x2ad7_d2bb);
    b = ii(b, c, d, a, word(k + 9), 21, 0xeb86_d391);
    a = addUnsigned(a, aa);
    b = addUnsigned(b, bb);
    c = addUnsigned(c, cc);
    d = addUnsigned(d, dd);
  }
  const wordToHex = (value: number) =>
    [0, 8, 16, 24]
      .map((shift) => `0${((value >>> shift) & 0xff).toString(16)}`.slice(-2))
      .join('');
  return `${wordToHex(a)}${wordToHex(b)}${wordToHex(c)}${wordToHex(d)}`;
}
