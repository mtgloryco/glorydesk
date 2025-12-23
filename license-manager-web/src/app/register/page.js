'use client';
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export default function RegisterPage() {
    const router = useRouter();
    useEffect(() => {
        router.push('/?auth=register');
    }, []);
    return <div className="container" style={{ textAlign: 'center', padding: '5rem' }}>Redirecting to registration...</div>;
}
