export const LICENSE_CONFIG = {
    plans: {
        'basic': {
            name: 'Basic Starter',
            durationDays: 30, // Monthly
            price: 1.99,
            tier: 'Basic',
            description: 'Digital Inventory Notebook (No POS)'
        },
        'medium': {
            name: 'Medium Plan',
            durationDays: 30, // Monthly
            price: 3.68,
            tier: 'Medium',
            description: 'Smart Cashier (POS + Receipts)'
        },
        'pro': {
            name: 'Pro Intelligence',
            durationDays: 30, // Monthly
            price: 4.99,
            tier: 'Pro',
            description: 'Complete Suite + Business Analytics'
        },
        'enterprise': {
            name: 'Enterprise',
            durationDays: 365,
            price: 0, // Contact Us
            tier: 'Enterprise',
            description: 'Unlimited Access + Priority Support'
        }
    },
    limits: {
        maxActivePerUser: 10
    }
};
