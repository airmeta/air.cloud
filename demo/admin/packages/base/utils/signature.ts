
import { Md5 } from "ts-md5";

/**
 * 签名生成工具类
 * 
 */
export class SignatureUtil {
    url: string;
    timestamp: string;
    ticket: string;
    nonce: string;
    appId: string;
    constructor(appId:string,url: string, timestamp: string, ticket: string, nonce: string) {
        this.appId = appId;
        this.url = url;
        this.timestamp = timestamp;
        this.ticket = ticket;
        this.nonce = nonce;
    }
    getSignString(method: string, request?: any): string {
        let signature = "";
        const maps = new Map<string, string>();
        maps.set("URL", this.url);
        maps.set("TIMESTAMP", this.timestamp);
        if (this.ticket) {
            maps.set("TICKET", this.ticket);
        }
        maps.set("APPID", this.appId);
        maps.set("NONCE", this.nonce);
        switch (method.toUpperCase()) {
            case "GET":
            case "DELETE":
                if (request.params) {
                    for (const key in Object.keys(request.params)) {
                        maps.set(key, request.params[key]);
                    }
                }
                break;
            case "POST":
            case "PUT":
                if (request.data!=null&&Object.keys(request.data).length > 0) {
                    const data1 = JSON.stringify(request.data);
                    maps.set("DATA", data1);
                }
                break;
        }
        const sortedKeys = Array.from(maps.keys()).sort();
        const sortedObj: any = {};
        sortedKeys.forEach((key) => {
            sortedObj[key] = maps.get(key);
        });
        console.log("Sorted Object for Signature:", JSON.stringify(sortedObj));
        signature = Md5.hashStr(JSON.stringify(sortedObj));
        return signature.toUpperCase();
    }
}