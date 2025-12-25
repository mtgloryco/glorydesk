import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { ObjectId } from 'mongodb';

export async function POST(request) {
    try {
        const client = await clientPromise;
        const db = client.db('license-manager'); // Use consistent DB name
        const body = await request.json();
        const { id } = body;

        if (!id) {
            return NextResponse.json({ error: 'Download ID is required' }, { status: 400 });
        }

        const result = await db.collection('downloads').updateOne(
            { _id: new ObjectId(id) },
            { $inc: { downloadCount: 1 } }
        );

        if (result.matchedCount === 0) {
            return NextResponse.json({ error: 'Download not found' }, { status: 404 });
        }

        return NextResponse.json({ success: true, count: result.modifiedCount });
    } catch (e) {
        console.error('Track download error:', e);
        return NextResponse.json({ error: e.message }, { status: 500 });
    }
}
