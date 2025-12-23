import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { LICENSE_CONFIG } from '@/lib/config';

export async function GET() {
    try {
        const client = await clientPromise;
        const db = client.db('license_manager');

        // Always return fresh plans from config to ensure UI matches
        const plans = Object.entries(LICENSE_CONFIG.plans).map(([key, plan]) => ({
            id: key,
            ...plan,
            featuresList: plan.tier === 'Premium'
                ? ['Hardware Binding', 'Bulk Stock Import', 'Excel Exporter', 'Advanced Reports']
                : ['Local DB Only', 'Max 50 Products', 'Standard Tracking']
        }));

        // Update DB in background if needed (optional)
        try {
            await db.collection('plans').deleteMany({});
            await db.collection('plans').insertMany(plans);
        } catch (e) { console.error('DB Sync Error:', e); }

        return NextResponse.json(plans);
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
