const http = require('http');

const options = {
    hostname: 'localhost',
    port: 3000,
    path: '/api/plans',
    method: 'GET',
};

const req = http.request(options, (res) => {
    let data = '';
    res.on('data', (chunk) => {
        data += chunk;
    });
    res.on('end', () => {
        console.log('Status Code:', res.statusCode);
        try {
            const plans = JSON.parse(data);
            console.log('Plans Count:', plans.length);
            console.log('Plans:', plans.map(p => ({ id: p.id, price: p.price, tier: p.tier })));
        } catch (e) {
            console.error('Failed to parse JSON:', data);
        }
    });
});

req.on('error', (e) => {
    console.error(`Problem with request: ${e.message}`);
});

req.end();
