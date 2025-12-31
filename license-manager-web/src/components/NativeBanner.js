'use client';

import { useEffect, useRef } from 'react';

export default function NativeBanner() {
    const containerRef = useRef(null);

    useEffect(() => {
        // Prevent double injection if the script is already there
        if (containerRef.current && containerRef.current.querySelector('script')) {
            return;
        }

        const script = document.createElement('script');
        script.src = "https://pl28374620.effectivegatecpm.com/ad0327c4527ce063da80e69c1b23be43/invoke.js";
        script.async = true;
        script.dataset.cfasync = "false";

        // Append the script to the container or body. 
        // Since the instructions imply the script finds the container by ID, 
        // appending it to the container itself is a safe way to keep them together.
        if (containerRef.current) {
            containerRef.current.appendChild(script);
        }

        return () => {
            // Optional cleanup if necessary, but ads usually shouldn't be removed aggressively
        };
    }, []);

    return (
        <div
            id="container-ad0327c4527ce063da80e69c1b23be43"
            ref={containerRef}
            style={{ minHeight: '1px' }} // Tiny layout placeholder to prevent layout shift if possible
        >
        </div>
    );
}
