export default function robots() {
    const baseUrl = 'https://itims.online'; // TODO: Update this to your actual domain

    return {
        rules: {
            userAgent: '*',
            allow: '/',
            disallow: ['/api/', '/dashboard/', '/admin/'],
        },
        sitemap: `${baseUrl}/sitemap.xml`,
    };
}
