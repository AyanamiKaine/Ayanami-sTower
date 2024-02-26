export async function load({ fetch, params }) {
	const res = await fetch('http://localhost:1337/api/software-engineering-posts?filters\[Slug\][$eq]=' + params.slug);
    const data = await res.json();

	return { data };
}
