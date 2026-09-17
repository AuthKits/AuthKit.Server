<script lang="ts">
  import { onMount } from "svelte";
  import { motion } from "@humanspeak/svelte-motion";
  import Header from "./components/layout/Header.svelte";
  import MethodDetail from "./components/method/MethodDetail.svelte";
  import MethodsColumn from "./components/navigation/MethodsColumn.svelte";
  import Sidebar from "./components/layout/Sidebar.svelte";
  import { api, connectionStatus, services, theme } from "./lib/stores";

  // Apply theme to html element
  $effect(() => {
    if (typeof document !== "undefined") {
      document.documentElement.setAttribute("data-theme", $theme);
    }
  });

  onMount(() => {
    void api.fetchServices().then(
      (list) => {
        $services = list;
        $connectionStatus = "connected";
      },
      () => {
        $connectionStatus = "error";
      },
    );
  });
</script>

<motion.div 
  class="flex h-screen flex-col overflow-hidden bg-bg text-text"
  initial={{ opacity: 0, scale: 0.95 }}
  animate={{ opacity: 1, scale: 1 }}
  transition={{ duration: 0.8, ease: "easeOut" }}
>
  <Header />
  <motion.main 
    class="flex min-h-0 flex-1 overflow-hidden"
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.6, delay: 0.2, type: "spring", stiffness: 150 }}
  >
    <Sidebar />
    <MethodsColumn />
    <motion.div 
      class="flex min-w-0 flex-1 overflow-hidden"
      initial={{ opacity: 0, x: 30, scale: 0.98 }}
      animate={{ opacity: 1, x: 0, scale: 1 }}
      transition={{ duration: 0.6, delay: 0.4, type: "spring", stiffness: 150 }}
    >
      <MethodDetail />
    </motion.div>
  </motion.main>
</motion.div>