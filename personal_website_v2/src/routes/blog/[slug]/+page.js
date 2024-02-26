export async function load({ fetch, params }) {
	const res = await fetch('http://localhost:1337/api/blog-posts?filters\[Slug\][$eq]=' + params.slug);
    const strapiJsonResponse = await res.json();

	return { strapiJsonResponse };
}
