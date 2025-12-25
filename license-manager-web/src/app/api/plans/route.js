import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { LICENSE_CONFIG } from '@/lib/config';

// GET: Fetch all plans
export async function GET() {
    try {
        const client = await clientPromise;
        const db = client.db('license_manager');
        const collection = db.collection('plans');

        let plans = await collection.find({}).toArray();

        // Auto-seed if empty
        if (plans.length === 0) {
            console.log('Seeding plans from config...');
            const seedData = Object.entries(LICENSE_CONFIG.plans).map(([key, plan]) => ({
                id: key,
                ...plan,
                features: plan.tier === 'Enterprise'
                    ? ['Unlimited Everything', 'Priority Support', 'Custom Integrations', 'Dedicated Account Manager']
                    : plan.tier === 'Pro'
                        ? ['POS System & Receipts', 'Business Analytics', 'Stock Forecasting', 'Profit & Loss Reports', 'Unlimited Products']
                        : plan.tier === 'Medium'
                            ? ['POS System', 'Digital Receipts', 'Basic Reports', 'Up to 500 Products', 'Stock IN/OUT']
                            : ['Digital Notebook', 'Stock Tracking', 'No POS Access', 'Max 50 Products', 'Local Backup'],
                createdAt: new Date(),
                updatedAt: new Date()
            }));

            await collection.insertMany(seedData);
            plans = await collection.find({}).toArray();
        }

        plans.sort((a, b) => a.price - b.price);

        const enterprise = plans.find(p => p.tier === 'Enterprise');
        if (enterprise) {
            plans = plans.filter(p => p.tier !== 'Enterprise');
            plans.push(enterprise);
        }

        return NextResponse.json(plans);
    } catch (error) {
        return NextResponse.json({ error: 'Failed to fetch plans' }, { status: 500 });
    }
}

// POST: Create a new plan
export async function POST(request) {
    try {
        const body = await request.json();
        const { id, name, price, tier, description, features, durationDays } = body;

        if (!id || !name || price === undefined || !tier) {
            return NextResponse.json({ error: 'Missing required fields' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        const existing = await db.collection('plans').findOne({ id });
        if (existing) {
            return NextResponse.json({ error: 'Plan ID already exists' }, { status: 400 });
        }

        const newPlan = {
            id, name, price: parseFloat(price), tier, description,
            features: Array.isArray(features) ? features : [],
            durationDays: parseInt(durationDays) || 30,
            createdAt: new Date(),
            updatedAt: new Date()
        };

        await db.collection('plans').insertOne(newPlan);
        return NextResponse.json({ success: true, plan: newPlan });
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}

// PUT: Update an existing plan
export async function PUT(request) {
    try {
        const body = await request.json();
        const { id, ...updates } = body;

        if (!id) return NextResponse.json({ error: 'Plan ID required' }, { status: 400 });

        const client = await clientPromise;
        const db = client.db('license_manager');

        updates.updatedAt = new Date();
        if (updates.price) updates.price = parseFloat(updates.price);
        if (updates.durationDays) updates.durationDays = parseInt(updates.durationDays);

        const result = await db.collection('plans').updateOne({ id }, { $set: updates });

        if (result.matchedCount === 0) return NextResponse.json({ error: 'Plan not found' }, { status: 404 });

        return NextResponse.json({ success: true });
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}

// DELETE: Remove a plan
export async function DELETE(request) {
    try {
        const { searchParams } = new URL(request.url);
        const id = searchParams.get('id');

        if (!id) return NextResponse.json({ error: 'Plan ID required' }, { status: 400 });

        const client = await clientPromise;
        const db = client.db('license_manager');

        const result = await db.collection('plans').deleteOne({ id });

        if (result.deletedCount === 0) return NextResponse.json({ error: 'Plan not found' }, { status: 404 });

        return NextResponse.json({ success: true });
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
