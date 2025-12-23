export const LICENSE_CONFIG = {
    plans: {
        'hobby': {
            name: 'Hobby Starter',
            durationDays: 30,
            price: 3.99,
            tier: 'Hobby',
            description: 'Digital Inventory Notebook (No POS)'
        },
        '1-month': {
            name: 'Professional Monthly',
            durationDays: 30,
            price: 5.99,
            tier: 'Professional',
            description: 'Full POS & Profit Analytics'
        },
        '1-year': {
            name: 'Enterprise Yearly',
            durationDays: 365,
            price: 60,
            tier: 'Enterprise',
            description: 'Best Value + Priority Support'
        }
    },
    limits: {
        maxActivePerUser: 10
    }
};
