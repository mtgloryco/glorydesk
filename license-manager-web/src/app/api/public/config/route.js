import { NextResponse } from "next/server";

export async function GET() {
    const config = {
        updateUrl: "https://github.com/itbienvenu/Releases",

        // You can add other feature flags here
        maintenanceMode: false,
        minVersionArgs: "1.2.1"
    };

    return NextResponse.json(config);
}
